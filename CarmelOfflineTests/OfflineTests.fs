namespace CarmelOfflineTests

open System
open System.Collections.Concurrent
open System.Collections.Generic
open System.Diagnostics
open System.Net
open System.Net.Http
open System.Net.Sockets
open System.Security.Cryptography
open System.Text
open System.Text.RegularExpressions
open System.Threading
open System.Threading.Tasks
open CarmelNet
open Microsoft.VisualStudio.TestTools.UnitTesting

[<AutoOpen>]
module Helpers =

    let saying<'T> (message: string) (expected: 'T, actual: 'T) =
        Assert.AreEqual<'T>(expected, actual, message)

    let equal<'T> (expected: 'T, actual: 'T) = saying "" (expected, actual)

    let isComplete (request: string) =
        match request.IndexOf "\r\n\r\n" with
        | -1 -> false
        | headerEnd ->
            let length = Regex.Match(request, @"(?im)^content-length:\s*(\d+)")
            not length.Success || request.Length >= headerEnd + 4 + int length.Groups[1].Value

    let rec readRequest (stream: NetworkStream) (soFar: string) : Task<string> =
        task {
            let buffer = Array.zeroCreate 16384
            let! count = stream.ReadAsync(buffer, 0, buffer.Length)
            let text = soFar + Encoding.ASCII.GetString(buffer, 0, count)
            if count = 0 || isComplete text then return text else return! readRequest stream text
        }

/// A local host recording each request. It sends answer, or with ValueNone holds the connection open without answering.
type Host(answer: string voption) =
    let listener = new TcpListener(IPAddress.Loopback, 0)
    let requests = ConcurrentQueue<string>()
    let firstRequest = TaskCompletionSource()
    let closing = TaskCompletionSource()
    do listener.Start()

    let serve (socket: TcpClient) : Task =
        task {
            use socket = socket
            use stream = socket.GetStream()
            let! request = readRequest stream ""
            requests.Enqueue request
            firstRequest.TrySetResult() |> ignore

            match answer with
            | ValueSome text ->
                let bytes = Encoding.ASCII.GetBytes text
                do! stream.WriteAsync(bytes, 0, bytes.Length)
            | ValueNone -> do! closing.Task
        }

    let rec accept () : Task =
        task {
            let! socket = listener.AcceptTcpClientAsync()
            serve socket |> ignore
            return! accept ()
        }

    do accept () |> ignore

    member _.Port = (listener.LocalEndpoint :?> IPEndPoint).Port
    member _.Requests = requests |> Seq.toList
    member _.FirstRequest: Task = firstRequest.Task

    interface IDisposable with
        member _.Dispose() =
            closing.TrySetResult() |> ignore
            listener.Stop()

[<AutoOpen>]
module Fixtures =

    let env = CarmelEnvironment.Sandbox
    let tokenText = "offline-token-value"
    let token = CarmelAccessToken.Token tokenText
    let accountNumber = "4242424242"
    let okAnswer = "HTTP/1.1 200 OK\r\nContent-Length: 2\r\nConnection: close\r\n\r\n{}"
    let refusedAnswer = "HTTP/1.1 502 Bad Gateway\r\nContent-Length: 0\r\nConnection: close\r\n\r\n"

    let redirectTo (port: int) =
        $"HTTP/1.1 307 Moved\r\nLocation: http://127.0.0.1:{port}/stolen\r\nContent-Length: 0\r\nConnection: close\r\n\r\n"

    /// Sends every request made in body to a local proxy. The vendor's addresses are https, so the proxy only ever
    /// sees a CONNECT and nothing leaves this machine.
    let throughLocalProxy (answer: string voption) (body: Host -> Task<'T>) : Task<'T> =
        task {
            use proxy = new Host(answer)
            let original = HttpClient.DefaultProxy
            let proxyUri = Uri $"http://127.0.0.1:{proxy.Port}/"
            HttpClient.DefaultProxy <- new WebProxy(proxyUri)

            try
                equal(proxyUri, HttpClient.DefaultProxy.GetProxy(Uri(env.Uri())))
                return! body proxy
            finally
                HttpClient.DefaultProxy <- original
        }

    let origination = Guid.Parse "11111111-1111-1111-1111-111111111111"
    let someId = Guid.Parse "22222222-2222-2222-2222-222222222222"

    let payment paymentId =
        (env, token, origination, paymentId, 100, CarmelTransferType.ACH CarmelSecCode.PPD, CarmelTransferDirection.Debit,
         "091809524", accountNumber, "Test Holder", CarmelBankAccountType.Checking, None)

    /// Every public call that makes an HTTP request through the generated client.
    let generatedClientCalls () =
        let run name (call: Async<Result<'T, exn * string>>) =
            name,
            async {
                match! call with
                | Ok _ -> return ValueNone
                | Error(err, _) -> return ValueSome err
            }

        [ run "getOriginationAccounts" (CarmelPayment.getOriginationAccounts (env, token))
          run "createCreditTransfer" (CarmelPayment.createCreditTransfer (payment "1"))
          run "fetchCreditTransfer" (CarmelPayment.fetchCreditTransfer (env, token, someId))
          run "fetchCreditTransfers" (CarmelPayment.fetchCreditTransfers (env, token, 1, None, None, None))
          run "approveCreditTransfer" (CarmelPayment.approveCreditTransfer (env, token, someId, CarmelApprovalAction.Approve))
          run "fetchEvents" (CarmelPayment.fetchEvents (env, token, 1, None, None, None))
          run "createWebhookSubscription" (CarmelWebhooks.createWebhookSubscription (env, token, "https://example.invalid/hook", CarmelWebhooks.webhookEvents))
          run "deleteWebhookSubscription" (CarmelWebhooks.deleteWebhookSubscription (env, token, someId))
          run "getWebhookSubscriptions" (CarmelWebhooks.getWebhookSubscriptions (env, token))
          run "getWebhookSubscription" (CarmelWebhooks.getWebhookSubscription (env, token, someId)) ]

/// A request as HttpClient handed it to its transport.
type SentRequest =
    { Headers: Map<string, string>
      Body: string }

/// Records the requests of this process from HttpClient's own diagnostic events, which come before TLS, so a request
/// for the vendor's https address can be read while a local proxy refuses to connect it anywhere.
type SentRequests() =
    let sent = ConcurrentQueue<SentRequest>()
    let subscriptions = ConcurrentQueue<IDisposable>()

    let record (event: KeyValuePair<string, obj>) =
        if event.Key = "System.Net.Http.Request" then
            let request = event.Value.GetType().GetProperty("Request").GetValue event.Value :?> HttpRequestMessage

            sent.Enqueue
                { Headers = request.Headers |> Seq.map (fun h -> h.Key.ToLowerInvariant(), String.Join(",", h.Value)) |> Map.ofSeq
                  Body = if isNull request.Content then "" else request.Content.ReadAsStringAsync().Result }

    let subscription =
        DiagnosticListener.AllListeners
        |> Observable.subscribe (fun listener ->
            if listener.Name = "HttpHandlerDiagnosticListener" then
                listener |> Observable.subscribe record |> subscriptions.Enqueue)

    member _.All = sent |> Seq.toList

    interface IDisposable with
        member _.Dispose() =
            subscription.Dispose()
            subscriptions |> Seq.iter _.Dispose()

/// What the public calls put on the wire, read without reaching the vendor.
[<TestClass>]
type ``Sent Request Tests`` () =

    let sending (calls: Async<unit> list) : Task<SentRequest list> =
        throughLocalProxy (ValueSome refusedAnswer) (fun _ ->
            task {
                use sent = new SentRequests()
                let! _ = calls |> List.map Async.Catch |> Async.Sequential |> Async.StartAsTask
                return sent.All
            })

    let dated (now: unit -> DateTime) paymentId =
        CarmelPayment.createCreditTransferDated now (payment paymentId) |> Async.Ignore

    let effectiveDate (request: SentRequest) =
        Regex.Match(request.Body, "\"effectiveDate\":\"([^\"]+)\"").Groups[1].Value

    [<TestMethod>]
    member _.``the webhook secret call sends the raw token`` () : Task =
        task {
            let! sent = sending [ CarmelWebhooks.getWebhookSubscriptionSecret (env, token, someId) |> Async.Ignore ]
            equal(1, sent.Length)
            equal("Bearer " + tokenText, sent.Head.Headers["authorization"])
        }

    [<TestMethod>]
    member _.``a generated-client call sends the raw token`` () : Task =
        task {
            let! sent = sending [ CarmelPayment.fetchCreditTransfer (env, token, someId) |> Async.Ignore ]
            equal(1, sent.Length)
            equal("Bearer " + tokenText, sent.Head.Headers["authorization"])
        }

    [<TestMethod>]
    member _.``a re-run payment sends the same key and body`` () : Task =
        task {
            let once = CarmelPayment.createCreditTransfer (payment "77") |> Async.Ignore
            let! sent = sending [ once; once ]
            equal(2, sent.Length)
            equal("p77", sent[0].Headers["idempotency-key"])
            equal("p77", sent[1].Headers["idempotency-key"])
            equal(sent[0].Body, sent[1].Body)
            StringAssert.Contains(sent[0].Body, accountNumber)
        }

    [<TestMethod>]
    [<DataRow("")>]
    [<DataRow("   ")>]
    member _.``a payment or subscription with a blank id is refused and sends nothing`` (blank: string) : Task =
        task {
            let! sent =
                sending
                    [ async { do! CarmelPayment.createCreditTransfer (payment blank) |> Async.Ignore }
                      async { do! CarmelWebhooks.createWebhookSubscriptionWithKey (env, token, blank, "https://example.invalid/hook", CarmelWebhooks.webhookEvents) |> Async.Ignore } ]
            equal(0, sent.Length)
            Assert.ThrowsException<Exception>(fun () -> CarmelPayment.createCreditTransfer (payment blank) |> ignore) |> ignore
            Assert.ThrowsException<Exception>(fun () ->
                CarmelWebhooks.createWebhookSubscriptionWithKey (env, token, blank, "https://example.invalid/hook", CarmelWebhooks.webhookEvents) |> ignore)
            |> ignore
        }

    [<TestMethod>]
    member _.``the README and the doc comment state the limit of a payment retry`` () =
        let root = IO.Path.Combine(__SOURCE_DIRECTORY__, "..")
        let readme = IO.File.ReadAllText(IO.Path.Combine(root, "README.md"))
        let source = IO.File.ReadAllText(IO.Path.Combine(root, "Carmel.fs"))
        StringAssert.Contains(readme, "safe to retry only while Carmel holds the key")
        StringAssert.Contains(readme, "second ACH")
        StringAssert.Contains(source, "retry can send a second ACH")

    [<TestMethod>]
    member _.``different payments send different keys`` () : Task =
        task {
            let! sent = sending [ CarmelPayment.createCreditTransfer (payment "1") |> Async.Ignore; CarmelPayment.createCreditTransfer (payment "2") |> Async.Ignore ]
            equal("p1", sent[0].Headers["idempotency-key"])
            equal("p2", sent[1].Headers["idempotency-key"])
        }

    [<TestMethod>]
    member _.``a re-run across midnight keeps the date of the first run`` () : Task =
        task {
            let days = ConcurrentQueue [ DateTime(2026, 11, 6, 23, 59, 59); DateTime(2026, 11, 7, 0, 0, 1); DateTime(2026, 11, 7, 0, 0, 2) ]
            let once = dated (fun () -> match days.TryDequeue() with | true, day -> day | _ -> failwith "no more days") "1"
            let! sent = sending [ once; once ]
            equal(2, sent.Length)
            equal("2026-11-06", effectiveDate sent[0])
            equal("2026-11-06", effectiveDate sent[1])
        }

    [<TestMethod>]
    member _.``a payment is dated when it first runs, not when it is built`` () : Task =
        task {
            let clock = ConcurrentQueue [ DateTime(2026, 11, 6) ]
            let once = dated (fun () -> match clock.TryDequeue() with | true, day -> day | _ -> failwith "no more days") "1"
            equal(1, clock.Count)
            let! sent = sending [ once ]
            equal(0, clock.Count)
            equal("2026-11-06", effectiveDate sent[0])
        }

/// Retry safety of the calls that change something.
[<TestClass>]
type ``Retry Safety Tests`` () =

    let sending (calls: Async<unit> list) : Task<SentRequest list> =
        throughLocalProxy (ValueSome refusedAnswer) (fun _ ->
            task {
                use sent = new SentRequests()
                let! _ = calls |> List.map Async.Catch |> Async.Sequential |> Async.StartAsTask
                return sent.All
            })

    let endpoint = "https://example.invalid/hook"

    let subscribe (url: string) (events: string[]) =
        CarmelWebhooks.createWebhookSubscription (env, token, url, events) |> Async.Ignore

    let key (request: SentRequest) = request.Headers["idempotency-key"]

    [<TestMethod>]
    member _.``a subscription sends a key, the same on a re-run and on a new call`` () : Task =
        task {
            let once = subscribe endpoint CarmelWebhooks.webhookEvents
            let! sent = sending [ once; once; subscribe endpoint CarmelWebhooks.webhookEvents ]
            equal(3, sent.Length)
            StringAssert.StartsWith(key sent[0], "w")
            equal(key sent[0], key sent[1])
            equal(key sent[0], key sent[2])
        }

    [<TestMethod>]
    member _.``the same events in another order send the same key`` () : Task =
        task {
            let! sent = sending [ subscribe endpoint [| "paymentOrder_sent"; "paymentOrder_failed" |]; subscribe endpoint [| "paymentOrder_failed"; "paymentOrder_sent" |] ]
            equal(key sent[0], key sent[1])
        }

    [<TestMethod>]
    member _.``another endpoint or other events send another key`` () : Task =
        task {
            let! sent =
                sending
                    [ subscribe endpoint [| "paymentOrder_sent" |]
                      subscribe "https://example.invalid/other" [| "paymentOrder_sent" |]
                      subscribe endpoint [| "paymentOrder_failed" |] ]

            equal(3, sent |> List.map key |> List.distinct |> List.length)
        }

    [<TestMethod>]
    member _.``a subscription with a key builds it from the caller's id`` () : Task =
        task {
            let! sent = sending [ CarmelWebhooks.createWebhookSubscriptionWithKey (env, token, "hook-7", endpoint, CarmelWebhooks.webhookEvents) |> Async.Ignore ]
            equal("whook-7", key sent.Head)
        }

    [<TestMethod>]
    member _.``an approval sends no key`` () : Task =
        task {
            let! sent = sending [ CarmelPayment.approveCreditTransfer (env, token, someId, CarmelApprovalAction.Approve) |> Async.Ignore ]
            Assert.IsFalse(sent.Head.Headers.ContainsKey "idempotency-key")
        }

    [<TestMethod>]
    member _.``a failed call that changes something is sent once`` () : Task =
        task {
            let! sent = sending [ CarmelPayment.approveCreditTransfer (env, token, someId, CarmelApprovalAction.Approve) |> Async.Ignore ]
            equal(1, sent.Length)
            let! sent = sending [ CarmelPayment.approveCreditTransfer (env, token, someId, CarmelApprovalAction.Cancel) |> Async.Ignore ]
            equal(1, sent.Length)
            let! sent = sending [ subscribe endpoint CarmelWebhooks.webhookEvents ]
            equal(1, sent.Length)
        }

    /// Carmel's docs give the key to all POST requests and not to other verbs. The docs state this; the bundled schema must agree.
    [<TestMethod>]
    member _.``the bundled schema has the key on every POST and on no PATCH`` () =
        use schema = System.Text.Json.JsonDocument.Parse(IO.File.ReadAllText(IO.Path.Combine(__SOURCE_DIRECTORY__, "..", "carmel-openapi-schema.json")))

        let hasKey (operation: System.Text.Json.JsonElement) =
            match operation.TryGetProperty "parameters" with
            | true, parameters -> parameters.EnumerateArray() |> Seq.exists (fun p -> p.GetProperty("name").GetString().ToLowerInvariant() = "idempotency-key")
            | _ -> false

        let operations =
            [ for path in schema.RootElement.GetProperty("paths").EnumerateObject() do
                  for verb in path.Value.EnumerateObject() do
                      verb.Name, path.Name, hasKey verb.Value ]

        let wrong =
            operations
            |> List.filter (fun (verb, _, keyed) ->
                match verb with
                | "post" -> not keyed
                | _ -> keyed)

        saying (sprintf "%A" wrong) (0, wrong.Length)
        Assert.IsTrue(operations |> List.exists (fun (verb, path, _) -> verb = "patch" && path = "/api/v1/payment-orders/{paymentOrderId}"))

/// A payment's account numbers stay on Carmel's host: two local hosts, the first redirecting to the second.
[<TestClass>]
type ``Redirect Tests`` () =

    let paymentOrder () =
        let account = JsonData.CarmelOpenApi.PaymentAccount(AccountNumber = accountNumber, Name = "Test Holder", RoutingNumber = "091809524")
        JsonData.CarmelOpenApi.CreatePaymentOrder(
            Amount = 100, EffectiveDate = "2026-01-01", OriginationAccountId = origination, Type = "ACH", Direction = "debit",
            ReceivingAccount = account, SupplementalData = Map [ "ExternalId", "1" ])

    let stopsAtRedirect () : Task =
        task {
            use other = new Host(ValueSome okAnswer)
            use first = new Host(ValueSome(redirectTo other.Port))
            use http = Utils.makeHttpClient env token
            http.BaseAddress <- Uri $"http://127.0.0.1:{first.Port}/"
            let client = JsonData.CarmelOpenApi.Client http
            let! result = client.PostApiV1PaymentOrders(paymentOrder (), "p1") |> Async.Catch |> Async.StartAsTask
            do! first.FirstRequest
            Assert.IsTrue(first.Requests.Head.Contains accountNumber)
            saying (sprintf "The other host got: %A" other.Requests) (0, other.Requests.Length)
            Assert.IsTrue(match result with Choice2Of2 _ -> true | Choice1Of2 _ -> false)
        }

    [<TestMethod>]
    member _.``a payment order isn't re-posted on a 307`` () = stopsAtRedirect ()

    [<TestMethod>]
    member _.``a payment order isn't re-posted on a 307, when logging`` () : Task =
        task {
            Utils.logUnsuccessfulHandler <- Some ignore

            try
                do! stopsAtRedirect ()
            finally
                Utils.logUnsuccessfulHandler <- None
        }

    [<TestMethod>]
    member _.``the token request isn't re-posted on a 307`` () : Task =
        task {
            use other = new Host(ValueSome okAnswer)
            use first = new Host(ValueSome(redirectTo other.Port))
            let! _ =
                Utils.makePostRequestWithHeadersAndTimeout Utils.ApplicationUrlForm $"http://127.0.0.1:{first.Port}/oauth2/token" "grant_type=client_credentials" []
                |> Async.Catch
                |> Async.StartAsTask
            do! first.FirstRequest
            saying (sprintf "The other host got: %A" other.Requests) (0, other.Requests.Length)
        }

/// A returned Async is a description of the call: running it again makes the call again.
[<TestClass>]
type ``Rerun Tests`` () =

    [<TestMethod>]
    member _.``every generated-client call sends again when re-run`` () : Task =
        throughLocalProxy (ValueSome refusedAnswer) (fun proxy ->
            task {
                let! failures =
                    generatedClientCalls ()
                    |> List.map (fun (name, call) ->
                        async {
                            let before = proxy.Requests.Length
                            let! first = call
                            let! second = call
                            let sent = proxy.Requests.Length - before
                            let disposed = [ first; second ] |> List.exists (function ValueSome(:? ObjectDisposedException) -> true | _ -> false)
                            return if sent = 2 && not disposed then None else Some $"{name}: sent {sent} of 2, disposed={disposed}, second={second}"
                        })
                    |> Async.Sequential
                    |> Async.StartAsTask

                let failed = failures |> Array.choose id
                saying (String.Join("\n", failed)) (0, failed.Length)
                proxy.Requests |> List.iter (fun request -> StringAssert.StartsWith(request, "CONNECT api-sandbox.carmelsolutions.com:443"))
            })

/// Cancelling the Async's own token stops the HTTP request.
[<TestClass>]
type ``Cancellation Tests`` () =

    let calls () =
        (generatedClientCalls () |> List.map (fun (name, call) -> name, Async.Ignore call))
        @ [ "getAcccessToken", Async.Ignore(CarmelPayment.getAcccessToken (env, "client", "secret"))
            "getWebhookSubscriptionSecret", Async.Ignore(CarmelWebhooks.getWebhookSubscriptionSecret (env, token, someId)) ]

    let cancelled (name: string, call: Async<unit>) (proxy: Host) : Task<string voption> =
        task {
            use cts = new CancellationTokenSource()
            let running = Async.StartAsTask(call, cancellationToken = cts.Token)
            do! Task.WhenAny(proxy.FirstRequest, running) :> Task
            cts.Cancel()
            do! Task.WhenAny(running, Task.Delay(TimeSpan.FromSeconds 2.)) :> Task
            return if running.IsCanceled then ValueNone else ValueSome $"{name}: {running.Status}"
        }

    let rec failures (remaining: (string * Async<unit>) list) (found: string list) : Task<string list> =
        task {
            match remaining with
            | [] -> return List.rev found
            | call :: rest ->
                let! outcome = throughLocalProxy ValueNone (cancelled call)
                return! failures rest (ValueOption.fold (fun all text -> text :: all) found outcome)
        }

    [<TestMethod>]
    member _.``cancelling stops every call`` () : Task =
        task {
            let! failed = failures (calls ()) []
            saying (String.Join("\n", failed)) (0, failed.Length)
        }

/// Printing a token doesn't show it; the request still carries it.
[<TestClass>]
type ``Secret Tests`` () =

    let hidden (printed: string) =
        Assert.IsFalse(printed.Contains tokenText, printed)

    [<TestMethod>]
    member _.``a token prints hidden`` () =
        hidden (token.ToString())
        hidden (string token)
        hidden $"{token}"
        hidden (sprintf "%A" token)
        hidden (sprintf "%O" token)
        hidden (sprintf "%A" [ token ])

    [<TestMethod>]
    member _.``the client sends the raw token`` () =
        use http = Utils.makeHttpClient env token
        equal(tokenText, http.DefaultRequestHeaders.Authorization.Parameter)

    [<TestMethod>]
    member _.``the caller reads the raw token by matching the case`` () =
        match token with
        | CarmelAccessToken.Token raw -> equal(tokenText, raw)

/// parseWebhookResponse against Svix's published test vector and forms around it.
[<TestClass>]
type ``Webhook Signature Tests`` () =

    let secret = "whsec_MfKQ9r8GKYqrTwjUPD8ILPZIo2LaLaSw"
    let svixId = "msg_p5jXN8AQM9LWM0D4loKWxJek"
    let timestamp = "1614265330"
    let signature = "g0hM9SsE+OTPJTGt/tmIKtSyZlE3uFJELVlNIOLJ1OE="
    let payload = "{\"test\": 2432232314}"
    let anyAge = 300_000.0

    let parse header =
        CarmelWebhooks.parseWebhookResponse (anyAge, secret, payload, svixId, timestamp, header)

    let accepted (header: string) = Assert.IsNotNull(parse header, header)

    /// A message correctly signed with the secret at the given time, checked with a half-hour tolerance.
    let signedAt (time: DateTimeOffset) =
        let seconds = time.ToUnixTimeSeconds().ToString()
        use hmac = new HMACSHA256(Convert.FromBase64String(secret.Split('_').[1]))
        let signed = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes $"{svixId}.{seconds}.{payload}"))
        CarmelWebhooks.parseWebhookResponse (0.5, secret, payload, svixId, seconds, $"v1,{signed}")

    let refused (header: string) =
        let raised = try parse header |> ignore; false with _ -> true
        Assert.IsTrue(raised, $"Accepted: {header}")

    [<TestMethod>]
    member _.``the published vector verifies`` () = accepted $"v1,{signature}"

    [<TestMethod>]
    member _.``any one of several signatures verifies`` () =
        accepted $"v1,bm90LXRoZS1zaWduYXR1cmU= v1,{signature}"
        accepted $"v1,{signature} v1,bm90LXRoZS1zaWduYXR1cmU="

    [<TestMethod>]
    member _.``a different signature is refused`` () =
        refused "v1,bm90LXRoZS1zaWduYXR1cmU="
        refused ""

    [<TestMethod>]
    member _.``a signature inside other text is refused`` () =
        refused $"v1,x{signature}"
        refused $"v1,{signature}x"
        refused signature
        refused $"junk{signature}junk"

    [<TestMethod>]
    member _.``a signature of an unknown version is refused`` () = refused $"v2,{signature}"

    [<TestMethod>]
    member _.``an old message is refused within the tolerance`` () =
        let raised =
            try
                CarmelWebhooks.parseWebhookResponse (0.5, secret, payload, svixId, timestamp, $"v1,{signature}") |> ignore
                false
            with _ -> true

        Assert.IsTrue raised

    [<TestMethod>]
    member _.``a fresh message signed with the secret verifies`` () =
        Assert.IsNotNull(signedAt DateTimeOffset.UtcNow)

    [<TestMethod>]
    member _.``a message signed slightly ahead verifies within the tolerance`` () =
        Assert.IsNotNull(signedAt (DateTimeOffset.UtcNow.AddMinutes 10.))

    [<TestMethod>]
    member _.``a validly signed message from beyond the tolerance ahead is refused`` () =
        let raised = try signedAt (DateTimeOffset.UtcNow.AddDays 30.) |> ignore; false with _ -> true
        Assert.IsTrue raised

    [<TestMethod>]
    member _.``the signature compare looks at every position`` () =
        let bytes = Encoding.ASCII.GetBytes signature
        let changedAt position = bytes |> Array.mapi (fun index b -> if index = position then b ^^^ 1uy else b)
        Assert.IsTrue(CarmelWebhooks.fixedTimeEquals bytes (Array.copy bytes))
        [ 0 .. bytes.Length - 1 ] |> List.iter (fun position -> Assert.IsFalse(CarmelWebhooks.fixedTimeEquals bytes (changedAt position), $"position {position}"))

    [<TestMethod>]
    member _.``the signature compare refuses another length`` () =
        let bytes = Encoding.ASCII.GetBytes signature
        Assert.IsFalse(CarmelWebhooks.fixedTimeEquals bytes bytes[.. bytes.Length - 2])
        Assert.IsFalse(CarmelWebhooks.fixedTimeEquals bytes [||])
        Assert.IsFalse(CarmelWebhooks.fixedTimeEquals bytes (Array.append bytes [| 0uy |]))
