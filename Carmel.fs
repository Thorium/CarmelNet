namespace CarmelNet

open System
open System.Net.Http

/// Carmel environments: Production and Sandbox
[<RequireQualifiedAccess>]
type CarmelEnvironment =
    | Sandbox
    | Production

    member this.Uri() =
        match this with
        | Sandbox -> "https://api-sandbox.carmelsolutions.com"
        | Production -> "https://api.carmelsolutions.com"

/// Access token to carmel services
[<RequireQualifiedAccess>]
type CarmelAccessToken =
    | Token of string

    override this.ToString() =
        match this with
        | Token x -> x


/// Direction: Credit (money to customer) or Debit (money from customer)
[<RequireQualifiedAccess>]
type CarmelTransferDirection =
    /// If you want to transfer money from your origination account to someone else's account this is known as a credit.
    | Credit
    /// if you want to transfer money from someone else's account to your origination account this is known as a debit.
    | Debit

    override this.ToString() =
        match this with
        | Credit -> "credit"
        | Debit -> "debit"

/// SubTransfer type for ACH: SEC-CODE: https://docs.carmelsolutions.com/reference/sec-codes
[<RequireQualifiedAccess>]
type CarmelSecCode =
    /// Internet Initiated Payment or Mobile Initiated Payment entry
    | WEB
    /// Telephone initiated entry
    | TEL
    /// Prearranged Payment and Deposit entry
    | PPD
    /// Corporate Credit or Debit
    | CCD

    override this.ToString() =
        match this with
        | PPD -> "PPD"
        | TEL -> "TEL"
        | WEB -> "WEB"
        | CCD -> "CCD"

/// Type: ACH transfer or Wire transfer
[<RequireQualifiedAccess>]
type CarmelTransferType =
    | ACH of CarmelSecCode
    | Wire

    override this.ToString() =
        match this with
        | ACH _ -> "ACH"
        | Wire -> "Wire"

/// Type: Target bank account type
[<RequireQualifiedAccess>]
type CarmelBankAccountType =
    | Checking
    | Savings

    override this.ToString() =
        match this with
        | Checking -> "checking"
        | Savings -> "savings"

/// Actions for status updates for payment order
[<RequireQualifiedAccess; Struct>]
type CarmelApprovalAction =
    /// Approve a payment order
    | Approve
    /// Cancel a payment order
    | Cancel

type CarmelWebhookResponse = JsonData.WebhookResponseRaw.Root

/// Carmel Webhook and Event types are the same.
module PaymentOrderType =
    /// This event occurs when a payment order is created through the REST API
    [<Literal>]
    let ApprovalRequired = "approvalRequired"
    /// This event occurs when a payment order is approved through the REST API
    [<Literal>]
    let Approved = "approved"
    /// This event occurs when a payment order has completed processing and Carmel has sent the payment order to the originating bank (ODFI).
    [<Literal>]
    let Sent = "sent"
    /// This event occurs when we receive an ACH or Wire Return from an RDFI (Receiving Financial Institution). For a credit payment order, the funds are returned to the ODFI. For a Debit payment order, the funds are returned to the RDFI.
    [<Literal>]
    let Returned = "returned"
    /// A corrected event occurs when an ACH NOC (Notice of Correction) is received from an RDFI (receiving bank or credit union). You may receive corrected events for correction codes C01 through C07 (except for C04).
    [<Literal>]
    let Corrected = "corrected"
    /// This event occurs when a payment order has been processed for remittance. Where configured, remittance is only applicable for ACH debit payment orders. In brief, it means that funds collected by a debit payment order are "remitted" (transferred) from the collection account to one or more different bank accounts. The remittance process will generate a "remitted" event with a negative remittance.amount value if the payment order was returned.
    [<Literal>]
    let Remitted = "remitted"
    /// This event occurs when a payment order can no longer be processed. This could be due to an issue with the payment order in our system or with the ODFI. We try hard to avoid this by validating most aspects of a payment order when it is created.
    [<Literal>]
    let Failed = "failed"
    /// This event occurs when a payment order you have cancelled a payment order through the REST API.
    [<Literal>]
    let Cancelled = "cancelled"

/// Types of payment order.
/// Annoyingly API uses 2 different forms: with and without a prefix of paymentOrder_
/// so you have to be extra careful.
[<RequireQualifiedAccess>]
type CarmelPaymentOrderType =
    /// paymentOrder_approvalRequired: This event occurs when a payment order is created through the REST API
    | ApprovalRequired
    /// paymentOrder_approved: This event occurs when a payment order is approved through the REST API
    | Approved
    /// paymentOrder_sent: This event occurs when a payment order has completed processing and Carmel has sent the payment order to the originating bank (ODFI).
    | Sent
    /// paymentOrder_returned: This event occurs when we receive an ACH or Wire Return from an RDFI (Receiving Financial Institution). For a credit payment order, the funds are returned to the ODFI. For a Debit payment order, the funds are returned to the RDFI.
    | Returned
    /// paymentOrder_corrected: A corrected event occurs when an ACH NOC (Notice of Correction) is received from an RDFI (receiving bank or credit union). You may receive corrected events for correction codes C01 through C07 (except for C04).
    | Corrected
    /// paymentOrder_remitted: This event occurs when a payment order has been processed for remittance. Where configured, remittance is only applicable for ACH debit payment orders. In brief, it means that funds collected by a debit payment order are "remitted" (transferred) from the collection account to one or more different bank accounts. The remittance process will generate a "remitted" event with a negative remittance.amount value if the payment order was returned.
    | Remitted
    /// paymentOrder_failed: This event occurs when a payment order can no longer be processed. This could be due to an issue with the payment order in our system or with the ODFI. We try hard to avoid this by validating most aspects of a payment order when it is created.
    | Failed
    /// paymentOrder_cancelled: This event occurs when a payment order you have cancelled a payment order through the REST API.
    | Cancelled

    /// PaymentOrderType.Type as string, without prefix of "paymentOrder_".
    /// As used in the actual JSON return of a webhook type code, e.g. { "type": "approvalRequired", ... }
    override this.ToString() =
        match this with
        | ApprovalRequired -> PaymentOrderType.ApprovalRequired
        | Approved -> PaymentOrderType.Approved
        | Sent -> PaymentOrderType.Sent
        | Returned -> PaymentOrderType.Returned
        | Corrected -> PaymentOrderType.Corrected
        | Remitted -> PaymentOrderType.Remitted
        | Failed -> PaymentOrderType.Failed
        | Cancelled -> PaymentOrderType.Cancelled

    /// PaymentOrderType.Type with a "paymentOrder_" prefix.
    /// As used in WebApi, events like registering subscriptions to webhooks.
    member this.ToWebHookEvent() =
        "paymentOrder_" + this.ToString()

module internal Utils =

    open System.IO
    open System.Net
    open System.Net.Http
    open System.Text

    let timeoutMs = 15000

    [<Struct>]
    type PostRequestTypes =
        | Text
        | ApplicationXml
        | ApplicationSoapXml
        | ApplicationJson
        | ApplicationJson_HTTP11
        | ApplicationUrlForm

    [<Struct; RequireQualifiedAccess>]
    type HttpVerb =
        | POST
        | GET
        | DELETE


    /// Make a post-web-request, with custom headers
    let makeVerbRequestWithHeadersAndTimeout
        (verb: HttpVerb)
        (reqType: PostRequestTypes)
        (url: string)
        (requestBody: string)
        headers
        =
        let timeout = timeoutMs // Timeout has to be smaller than DTC timeout

        let reqtype =
            match reqType with
            | Text -> "text/xml; charset=utf-8"
            | ApplicationXml -> "application/xml; charset=utf-8"
            | ApplicationSoapXml -> "application/soap+xml; charset=utf-8"
            | ApplicationJson -> "application/json"
            | ApplicationJson_HTTP11 -> "application/json"
            | ApplicationUrlForm -> "application/x-www-form-urlencoded"

        let asynccall =
            async {
                let! res =
                    async {
                        use client = new HttpClient()
                        client.Timeout <- TimeSpan.FromMilliseconds(float timeout)

                        use request =
                            new HttpRequestMessage(
                                (match verb with
                                 | HttpVerb.POST -> HttpMethod.Post
                                 | HttpVerb.GET -> HttpMethod.Get
                                 | HttpVerb.DELETE -> HttpMethod.Delete),
                                url
                            )

                        request.Version <-
                            match reqType with
                            | ApplicationJson_HTTP11 -> Version(1, 1)
                            | _ -> Version(1, 0)

                        headers
                        |> Seq.iter (fun (h: string, k: string) ->
                            if not (String.IsNullOrEmpty h) then
                                request.Headers.TryAddWithoutValidation(h, k) |> ignore)

                        if not (String.IsNullOrEmpty requestBody) then
                            request.Content <- new StringContent(requestBody, Encoding.ASCII, reqtype)

                        let! response = client.SendAsync(request) |> Async.AwaitTask
                        let! rdata = response.Content.ReadAsStringAsync() |> Async.AwaitTask

                        if response.IsSuccessStatusCode then
                            return rdata
                        else
                            return failwith rdata
                    }
                    |> Async.Catch

                match res with
                | Choice1Of2 x -> return x, None
                | Choice2Of2 e when not (String.IsNullOrEmpty e.Message) ->
                    return e.Message, Some e
                | Choice2Of2 e ->
                    match e with
                    | :? WebException as wex when not (isNull wex.Response) ->
                        use stream = wex.Response.GetResponseStream()
                        use reader = new StreamReader(stream)
                        let err = reader.ReadToEnd()
                        return err, Some e
                    | :? TimeoutException as e -> return failwith (e.ToString())
                    | _ ->
                        // System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(e).Throw()
                        return failwith (e.ToString())
            }

        asynccall
    //  |> Async.StartImmediateAsTask |> Task.WaitAll

    let makePostRequestWithHeadersAndTimeout (reqType: PostRequestTypes) (url: string) (requestBody: string) headers =
        makeVerbRequestWithHeadersAndTimeout HttpVerb.POST reqType url requestBody headers

    let makeGetRequestWithHeaders (reqType: PostRequestTypes) (url: string) headers =
        makeVerbRequestWithHeadersAndTimeout HttpVerb.GET reqType url "" headers


    let rec internal getErrorDetails: Exception -> string =
        function
        | :? Swagger.OpenApiException as e ->
            let content = e.Content.ReadAsStringAsync() |> Async.AwaitTask |> Async.RunSynchronously
            content
        | :? AggregateException as aex -> getErrorDetails (aex.GetBaseException())
        | :? WebException as wex when not (isNull wex.Response) ->
            use stream = wex.Response.GetResponseStream()
            use reader = new System.IO.StreamReader(stream)
            let err = reader.ReadToEnd()
            err
        | :? TimeoutException as e -> "Timeout"
        | e -> e.Message

    let mutable logUnsuccessfulHandler: Option<System.Net.HttpStatusCode * string -> Unit> =
        None

    let internal makeHttpClient (env: CarmelEnvironment) (access_token: CarmelAccessToken) =
        let httpClient =
            if logUnsuccessfulHandler.IsNone then
                new System.Net.Http.HttpClient(BaseAddress = Uri(env.Uri()))
            else
                let handler1 = new HttpClientHandler(UseCookies = false)
                let handler2 = new JsonData.ErrorHandler(handler1)
                new System.Net.Http.HttpClient(handler2, true, BaseAddress = Uri(env.Uri()))

        httpClient.DefaultRequestHeaders.Authorization <-
            Headers.AuthenticationHeaderValue("Bearer", access_token.ToString())

        httpClient.DefaultRequestHeaders.Add("User-Agent", "F# Client")
        httpClient

/// Documentation: https://docs.carmelsolutions.com/reference/introduction
module CarmelPayment =

    open Utils
    open FSharp.Data
    open FSharp.Data.JsonProvider

    let auth_server = "https://auth.carmelsolutions.com"
    let token_resource = auth_server + "/oauth2/token"

    let scope = "/pay"

    /// Get OAuth2 token for calling the services
    let getAcccessToken (env: CarmelEnvironment, clientId: string, clientSecret: string) =
        let carmelAuthHeader =
            [ "Authorization",
              "Basic "
              + (($"{clientId}:{clientSecret}")
                 |> System.Text.Encoding.UTF8.GetBytes
                 |> Convert.ToBase64String)
              "Accept", "application/json" ]

        let authBody =
            $"""grant_type=client_credentials&scope={System.Web.HttpUtility.UrlEncode(env.Uri() + scope)}"""

        async {
            let! res, exn =
                Utils.makePostRequestWithHeadersAndTimeout ApplicationUrlForm token_resource authBody carmelAuthHeader

            match res, exn with
            | r, Some err -> return raise err
            | tokenResponse, None ->
                let isOk, token =
                    System.Text.Json.JsonSerializer
                        .Deserialize<System.Text.Json.JsonElement>(tokenResponse)
                        .TryGetProperty "access_token"

                if isOk then
                    return token.ToString() |> CarmelAccessToken.Token
                else
                    return failwith $"No access_token gotten {tokenResponse}"
        }

    /// Gets company internal accounts
    let getOriginationAccounts (env: CarmelEnvironment, access_token: CarmelAccessToken) =
        let httpClient = makeHttpClient env access_token
        let client = JsonData.CarmelOpenApi.Client httpClient

        async {

            let subscription =
                match logUnsuccessfulHandler with | Some v -> Some(JsonData.reportUnsuccessfulEvents v) | None -> None

            let! res = client.GetApiV1OriginationAccounts() |> Async.Catch
            httpClient.Dispose()

            match subscription with | Some v -> v.Dispose() | None -> ()

            match res with
            | Choice1Of2 x -> return Ok x.OriginationAccounts
            | Choice2Of2 err ->
                let details = getErrorDetails err

                //printfn "Used signature: %s" signature_bodyhash_string
                return Error(err, details)
        }


    //let getOriginationAccount(env:CarmelEnvironment, access_token:CarmelAccessToken) =
    //    let authHeader = ["Authorization", $"Bearer {access_token}"; "User-Agent", "F# Client"]
    //    async {

    //        let! res, exn = Utils.makeGetRequestWithHeaders ApplicationJson_HTTP11 (env.Uri() + "/api/v1/origination-accounts") authHeader
    //        match res, exn with
    //        | r, Some err -> return raise err
    //        | origination, None ->
    //            let oris = JsonData.OriginationAccountsResponse.Load (Serializer.Deserialize origination)
    //            return oris
    //    }

    //let createCreditTransfer (env:CarmelEnvironment, access_token:CarmelAccessToken, originationAccount:Guid, paymentId:string, amountCents:int, transferType:CarmelTransferType, direction:CarmelTransferDirection, routingNumber:string, accountNumber, accountHolderName, accountType:CarmelBankAccountType, address1:Option<string>) =

    //    match direction, transferType with
    //    | CarmelTransferDirection.Credit, CarmelTransferType.ACH CarmelSecCode.WEB
    //    | CarmelTransferDirection.Credit, CarmelTransferType.ACH CarmelSecCode.TEL -> failwith "Invalid transfer type. ACH Credit has to be PPD or CCD"
    //    | CarmelTransferDirection.Debit, CarmelTransferType.Wire -> failwith "Wire transfers must be type of credit"
    //    | _ -> ()

    //    if routingNumber.Length <> 9 then failwith "Invalid routing number length, has to be 9 chars."
    //    let accountHolderName =
    //       if accountHolderName.Length > 22 then accountHolderName.Substring(0,22)
    //       else accountHolderName

    //    let subType =
    //        match transferType with
    //        | CarmelTransferType.ACH secCode ->
    //            Some (secCode.ToString())
    //        | CarmelTransferType.Wire -> None

    //    match address1 with
    //    | None when transferType = CarmelTransferType.Wire -> failwith "Wire transfer: Address 1 has to be defined."
    //    | _ -> ()

    //    let paymentRequest =
    //        JsonData.PaymentOrderRequest.Root(
    //            transferType.ToString(), direction.ToString(),
    //            JsonData.PaymentOrderRequest.ReceivingAccount(
    //                accountNumber,
    //                accountHolderName,
    //                routingNumber,
    //                Some (accountType.ToString()),
    //                address1, None, None),
    //            amountCents (*amount cents*), DateTime.Now.Date (*effective date*),
    //            JsonData.PaymentOrderRequest.StringOrGuid(originationAccount.ToString()), subType, None, None).JsonValue |> Serializer.Serialize

    //    let headers = ["Authorization", $"Bearer {access_token}"; "User-Agent", "F# Client"; "Idempotency-Key", $"p{paymentId}"]

    //    async {
    //        let! res, exn = Utils.makePostRequestWithHeadersAndTimeout ApplicationJson paymentRequest (env.Uri() + "/api/v1/payment-orders") headers
    //        match res, exn with
    //        | r, Some err -> return raise err
    //        | paymentOrder, None ->
    //            let ord = JsonData.PaymentOrderResponse.Load (Serializer.Deserialize paymentOrder)
    //            return ord.PaymentOrder
    //    }

    /// Initiate a money transfer
    let createCreditTransfer
        (
            env: CarmelEnvironment,
            access_token: CarmelAccessToken,
            origAccId: Guid,
            paymentId: string,
            amountCents: int,
            transferType: CarmelTransferType,
            direction: CarmelTransferDirection,
            routingNumber: string,
            accountNumber,
            accountHolderName: string,
            accountType: CarmelBankAccountType,
            address1: Option<string>
        ) =

        match direction, transferType with
        | CarmelTransferDirection.Credit, CarmelTransferType.ACH CarmelSecCode.WEB
        | CarmelTransferDirection.Credit, CarmelTransferType.ACH CarmelSecCode.TEL ->
            failwith "Invalid transfer type. ACH Credit has to be PPD or CCD"
        | CarmelTransferDirection.Debit, CarmelTransferType.Wire -> failwith "Wire transfers must be type of credit"
        | _ -> ()

        if routingNumber.Length <> 9 then
            failwith "Invalid routing number length, has to be 9 chars."

        let accountHolderName =
            if accountHolderName.Length > 22 then
                accountHolderName.Substring(0, 22)
            else
                accountHolderName

        let httpClient = makeHttpClient env access_token
        let client = JsonData.CarmelOpenApi.Client httpClient

        async {

            let subscription =
                match logUnsuccessfulHandler with | Some v -> Some(JsonData.reportUnsuccessfulEvents v) | None -> None

            let targetAccount =
                JsonData.CarmelOpenApi.PaymentAccount(
                    accountNumber,
                    accountHolderName,
                    routingNumber
                //, address1, address2, address3
                )

            targetAccount.Type <- accountType.ToString()

            match address1 with
            | None ->
                if transferType <> CarmelTransferType.Wire then
                    ()
                else
                    failwith "Wire transfer: Address 1 has to be defined."
            | Some a -> targetAccount.Address1 <- a

            let suplementdata = Map ["ExternalId", paymentId]
            let createPaymentOrder =
                JsonData.CarmelOpenApi.CreatePaymentOrder(
                    amountCents,
                    DateTime.Now.ToString("yyyy-MM-dd"),
                    origAccId,
                    transferType.ToString(),
                    direction.ToString(),
                    targetAccount,
                    suplementdata
                //, subtype
                )

            match transferType with
            | CarmelTransferType.ACH secCode -> createPaymentOrder.SubType <- secCode.ToString()
            | CarmelTransferType.Wire -> ()

            let idempotencyKey = $"p{paymentId}"

            let! res = client.PostApiV1PaymentOrders(createPaymentOrder, idempotencyKey) |> Async.Catch
            httpClient.Dispose()

            match subscription with | Some v -> v.Dispose() | None -> ()

            match res with
            | Choice1Of2 x -> return Ok x.PaymentOrder
            | Choice2Of2 err ->
                let details = getErrorDetails err

                //printfn "Used signature: %s" signature_bodyhash_string
                return Error(err, details)
        }

    /// Get the status of a money transfer
    let fetchCreditTransfer (env: CarmelEnvironment, access_token: CarmelAccessToken, paymentOrderId: Guid) =

        let httpClient = makeHttpClient env access_token
        let client = JsonData.CarmelOpenApi.Client httpClient

        async {

            let subscription =
                match logUnsuccessfulHandler with | Some v -> Some(JsonData.reportUnsuccessfulEvents v) | None -> None

            let! res = client.GetApiV1PaymentOrder paymentOrderId |> Async.Catch
            httpClient.Dispose()

            match subscription with | Some v -> v.Dispose() | None -> ()

            match res with
            | Choice1Of2 x -> return Ok x.PaymentOrder
            | Choice2Of2 err ->
                let details = getErrorDetails err

                //printfn "Used signature: %s" signature_bodyhash_string
                return Error(err, details)
        }

    /// Get a list of money transfer transactions
    let fetchCreditTransfers
        (
            env: CarmelEnvironment,
            access_token: CarmelAccessToken,
            pageId,
            orderStatus: Option<string>,
            startDate: Option<DateTime>,
            endDate: Option<DateTime>
        ) =

        let httpClient = makeHttpClient env access_token
        let client = JsonData.CarmelOpenApi.Client httpClient
        let pgSize = 30 // Page size: Little bit more than default 10, but not too heavy load

        async {

            let subscription =
                match logUnsuccessfulHandler with | Some v -> Some(JsonData.reportUnsuccessfulEvents v) | None -> None

            let! res =
                match orderStatus, startDate, endDate with
                | Some ost, Some std, Some edd ->
                    client.GetApiV1PaymentOrders(
                        pageId,
                        ost,
                        std.ToString "yyyy-MM-dd",
                        edd.ToString "yyyy-MM-dd",
                        Some pgSize
                    )
                    |> Async.Catch
                | Some ost, Some std, None ->
                    client.GetApiV1PaymentOrders(pageId, ost, std.ToString "yyyy-MM-dd", pageSize = Some pgSize)
                    |> Async.Catch
                | Some ost, None, Some edd ->
                    client.GetApiV1PaymentOrders(
                        pageId,
                        ost,
                        endDate = edd.ToString("yyyy-MM-dd"),
                        pageSize = Some pgSize
                    )
                    |> Async.Catch
                | Some ost, None, None ->
                    client.GetApiV1PaymentOrders(pageId, ost, pageSize = Some pgSize) |> Async.Catch
                | None, Some std, Some edd ->
                    client.GetApiV1PaymentOrders(
                        pageId,
                        startDate = std.ToString("yyyy-MM-dd"),
                        endDate = edd.ToString("yyyy-MM-dd"),
                        pageSize = Some pgSize
                    )
                    |> Async.Catch
                | None, Some std, None ->
                    client.GetApiV1PaymentOrders(pageId, startDate = std.ToString("yyyy-MM-dd"), pageSize = Some pgSize)
                    |> Async.Catch
                | None, None, Some edd ->
                    client.GetApiV1PaymentOrders(pageId, endDate = edd.ToString("yyyy-MM-dd"), pageSize = Some pgSize)
                    |> Async.Catch
                | None, None, None -> client.GetApiV1PaymentOrders(pageId, pageSize = Some pgSize) |> Async.Catch

            httpClient.Dispose()

            match subscription with | Some v -> v.Dispose() | None -> ()

            match res with
            | Choice1Of2 x -> return Ok x
            | Choice2Of2 err ->
                let details = getErrorDetails err

                //printfn "Used signature: %s" signature_bodyhash_string
                return Error(err, details)
        }

    /// Change a money transfer from approval required to aproved (or cancelled)
    let approveCreditTransfer
        (
            env: CarmelEnvironment,
            access_token: CarmelAccessToken,
            paymentOrderId: Guid,
            action: CarmelApprovalAction
        ) =

        let httpClient = makeHttpClient env access_token
        let client = JsonData.CarmelOpenApi.Client httpClient

        async {

            let subscription =
                match logUnsuccessfulHandler with | Some v -> Some(JsonData.reportUnsuccessfulEvents v) | None -> None

            let operationArray =
                [| JsonData.CarmelOpenApi.Operation(
                       (match action with
                        | CarmelApprovalAction.Approve -> "approved"
                        | CarmelApprovalAction.Cancel -> "cancelled"),
                       "/status",
                       "replace"
                   ) |]

            let! res = client.PatchApiV1PaymentOrder(paymentOrderId, operationArray) |> Async.Catch
            httpClient.Dispose()

            match subscription with | Some v -> v.Dispose() | None -> ()

            match res with
            | Choice1Of2 x -> return Ok x.PaymentOrder
            | Choice2Of2 err ->
                let details = getErrorDetails err

                //printfn "Used signature: %s" signature_bodyhash_string
                return Error(err, details)
        }

    /// Get list of events
    let fetchEvents
        (
            env: CarmelEnvironment,
            access_token: CarmelAccessToken,
            pageId: int,
            eventType: Option<string>,
            startDate: Option<DateTime>,
            endDate: Option<DateTime>
        ) =

        let httpClient = makeHttpClient env access_token
        let client = JsonData.CarmelOpenApi.Client httpClient
        let pgSize = 30 // Page size: Little bit more than default 10, but not too heavy load

        async {

            let subscription =
                match logUnsuccessfulHandler with | Some v -> Some(JsonData.reportUnsuccessfulEvents v) | None -> None

            let! res =
                match eventType, startDate, endDate with
                | Some et, Some std, Some edd ->
                    client.GetApiV1Events(
                        pageId,
                        et,
                        std.ToString "yyyy-MM-dd",
                        edd.ToString "yyyy-MM-dd",
                        Some pgSize
                    )
                    |> Async.Catch
                | Some et, Some std, None ->
                    client.GetApiV1Events(pageId, et, std.ToString "yyyy-MM-dd", pageSize = Some pgSize)
                    |> Async.Catch
                | Some et, None, Some edd ->
                    client.GetApiV1Events(pageId, et, endDate = edd.ToString("yyyy-MM-dd"), pageSize = Some pgSize)
                    |> Async.Catch
                | Some et, None, None -> client.GetApiV1Events(pageId, et, pageSize = Some pgSize) |> Async.Catch
                | None, Some std, Some edd ->
                    client.GetApiV1Events(
                        pageId,
                        startDate = std.ToString("yyyy-MM-dd"),
                        endDate = edd.ToString("yyyy-MM-dd"),
                        pageSize = Some pgSize
                    )
                    |> Async.Catch
                | None, Some std, None ->
                    client.GetApiV1Events(pageId, startDate = std.ToString("yyyy-MM-dd"), pageSize = Some pgSize)
                    |> Async.Catch
                | None, None, Some edd ->
                    client.GetApiV1Events(pageId, endDate = edd.ToString("yyyy-MM-dd"), pageSize = Some pgSize)
                    |> Async.Catch
                | None, None, None -> client.GetApiV1Events(pageId, pageSize = Some pgSize) |> Async.Catch

            httpClient.Dispose()

            match subscription with | Some v -> v.Dispose() | None -> ()

            match res with
            | Choice1Of2 x -> return Ok x
            | Choice2Of2 err ->
                let details = getErrorDetails err

                //printfn "Used signature: %s" signature_bodyhash_string
                return Error(err, details)
        }

module CarmelWebhooks =

    open Utils
    open CarmelPayment
    open FSharp.Data
    open FSharp.Data.JsonProvider

    /// List of different types of webhooks
    let webhookEvents =
        Microsoft.FSharp.Reflection.FSharpType.GetUnionCases(typeof<CarmelPaymentOrderType>)
        |> Array.map(fun x -> (Microsoft.FSharp.Reflection.FSharpValue.MakeUnion(x, [||]) :?> CarmelPaymentOrderType).ToWebHookEvent())

    //let createWebhookSubscription (env:CarmelEnvironment, access_token:CarmelAccessToken, endpointUrl:string, webhookEvents:string[]) =
    //    let subscribeRequest =
    //        JsonData.WebhookSubscriptionRequest.Root(endpointUrl, webhookEvents).JsonValue |> Serializer.Serialize

    //    let authHeader = ["Authorization", $"Bearer {access_token}"; "User-Agent", "F# Client"]

    //    async {
    //        let! res, exn = Utils.makePostRequestWithHeadersAndTimeout ApplicationJson subscribeRequest (env.Uri() + "/api/v1/web-hooks") authHeader
    //        match res, exn with
    //        | r, Some err -> return raise err
    //        | webhookresp, None ->
    //            let wr = JsonData.WebhookSubscriptionResponse.Load (Serializer.Deserialize webhookresp)
    //            return wr
    //    }

    /// Register a webhook listener
    let createWebhookSubscription
        (
            env: CarmelEnvironment,
            access_token: CarmelAccessToken,
            endpointUrl: string,
            webhookEvents: string[]
        ) =

        let httpClient = makeHttpClient env access_token
        let client = JsonData.CarmelOpenApi.Client httpClient

        async {

            let subscription =
                match logUnsuccessfulHandler with | Some v -> Some(JsonData.reportUnsuccessfulEvents v) | None -> None

            let createSubscriber =
                JsonData.CarmelOpenApi.CreateSubscriber(endpointUrl, webhookEvents)

            let! res = client.PostApiV1WebHooks createSubscriber |> Async.Catch
            httpClient.Dispose()

            match subscription with | Some v -> v.Dispose() | None -> ()

            match res with
            | Choice1Of2 x -> return Ok x
            | Choice2Of2 err ->
                let details = getErrorDetails err

                //printfn "Used signature: %s" signature_bodyhash_string
                return Error(err, details)
        }

    //let deleteWebhookSubscription (env:CarmelEnvironment, access_token:CarmelAccessToken, subscriberId:Guid) =

    //    let authHeader = ["Authorization", $"Bearer {access_token}"; "User-Agent", "F# Client"]
    //    let durl = env.Uri() + $"/api/v1/web-hooks/{subscriberId}"

    //    async {
    //        let! res, exn =
    //            Utils.makeVerbRequestWithHeadersAndTimeout HttpVerb.DELETE ApplicationJson durl "" authHeader

    //        match res, exn with
    //        | r, Some err -> return raise err
    //        | webhookresp, None ->
    //            let wr = JsonData.WebhookSubscriptionResponse.Load (Serializer.Deserialize webhookresp)
    //            return wr
    //    }

    /// Remove a webhook listener
    let deleteWebhookSubscription (env: CarmelEnvironment, access_token: CarmelAccessToken, subscriberId: Guid) =

        let httpClient = makeHttpClient env access_token
        let client = JsonData.CarmelOpenApi.Client httpClient

        async {

            let subscription =
                match logUnsuccessfulHandler with | Some v -> Some(JsonData.reportUnsuccessfulEvents v) | None -> None

            let! res = client.DeleteApiV1WebHook subscriberId |> Async.Catch
            httpClient.Dispose()

            match subscription with | Some v -> v.Dispose() | None -> ()

            match res with
            | Choice1Of2 x -> return Ok x
            | Choice2Of2 err ->
                let details = getErrorDetails err

                //printfn "Used signature: %s" signature_bodyhash_string
                return Error(err, details)
        }


    //let getWebhookSubscriptions (env:CarmelEnvironment, access_token:CarmelAccessToken) =

    //    let authHeader = ["Authorization", $"Bearer {access_token}"; "User-Agent", "F# Client"]
    //    let gurl = env.Uri() + $"/api/v1/web-hooks"

    //    async {
    //        let! res, exn =
    //            Utils.makeGetRequestWithHeaders ApplicationJson gurl authHeader

    //        match res, exn with
    //        | r, Some err -> return raise err
    //        | webhookresp, None ->
    //            let wr = JsonData.WebhookSubscriptionResponse.Load (Serializer.Deserialize webhookresp)
    //            return wr
    //    }

    /// Get a list of webhook listeners
    let getWebhookSubscriptions (env: CarmelEnvironment, access_token: CarmelAccessToken) =

        let httpClient = makeHttpClient env access_token
        let client = JsonData.CarmelOpenApi.Client httpClient

        async {

            let subscription =
                match logUnsuccessfulHandler with | Some v -> Some(JsonData.reportUnsuccessfulEvents v) | None -> None

            let! res = client.GetApiV1WebHooks() |> Async.Catch
            httpClient.Dispose()

            match subscription with | Some v -> v.Dispose() | None -> ()

            match res with
            | Choice1Of2 x -> return Ok x
            | Choice2Of2 err ->
                let details = getErrorDetails err

                //printfn "Used signature: %s" signature_bodyhash_string
                return Error(err, details)
        }

    /// Fetch details of a webhook listener
    let getWebhookSubscription (env: CarmelEnvironment, access_token: CarmelAccessToken, subscriberId: Guid) =

        let httpClient = makeHttpClient env access_token
        let client = JsonData.CarmelOpenApi.Client httpClient

        async {

            let subscription =
                match logUnsuccessfulHandler with | Some v -> Some(JsonData.reportUnsuccessfulEvents v) | None -> None

            let! res = client.GetApiV1WebHook(subscriberId) |> Async.Catch
            httpClient.Dispose()

            match subscription with | Some v -> v.Dispose() | None -> ()

            match res with
            | Choice1Of2 x -> return Ok x
            | Choice2Of2 err ->
                let details = getErrorDetails err

                //printfn "Used signature: %s" signature_bodyhash_string
                return Error(err, details)
        }

    /// Get a webhook listener secret, that is used to calculate and verify the webhook svix signatures.
    let getWebhookSubscriptionSecret (env: CarmelEnvironment, access_token: CarmelAccessToken, subscriberId: Guid) =

        let authHeader =
            [ "Authorization", $"Bearer {access_token}"; "User-Agent", "F# Client" ]

        let surl = env.Uri() + $"/api/v1/web-hooks/{subscriberId}/secret"

        async {
            let! res, exn = Utils.makeGetRequestWithHeaders ApplicationJson surl authHeader

            match res, exn with
            | r, Some err -> return raise err
            | webhookresp, None ->
                let wr =
                    JsonData.WebhookSubscriptionSecretResponseRaw.Load(Serializer.Deserialize webhookresp)

                return wr.Secret
        }

//let getWebhookSubscriptionSecret (env:CarmelEnvironment, access_token:CarmelAccessToken, subscriberId:Guid) =

//    use httpClient = makeHttpClient env access_token
//    let client = JsonData.CarmelOpenApi.Client httpClient
//    async {

//        let subscription =
//            if logUnsuccessfulHandler.IsSome then
//                Some (JsonData.reportUnsuccessfulEvents logUnsuccessfulHandler.Value)
//            else None

//        let! res = client.GetApiV1WebHookSecret subscriberId |> Async.Catch
//        httpClient.Dispose()
//        if subscription.IsSome then
//            subscription.Value.Dispose()
//        match res with
//        | Choice1Of2 x -> return Ok x // schema is wrong.
//        | Choice2Of2 err ->
//            let details = getErrorDetails err

//            //printfn "Used signature: %s" signature_bodyhash_string
//            return Error(err, details)
//    }

    
    /// Verifies the signature and parses the webhook response. Parameters:
    /// acceptedToleranceHours: How old signatures are accepted. Notice possible time-zone differences.
    /// webhookSecret you'll get with getWebhookSubscriptionSecret call.
    /// response_content is the raw content of the POST request. Note: response_content has to be in raw response format, not pretty JSON.
    /// Svix values are available in the message headers: svix-id svix-timestamp svix-signature
    let parseWebhookResponse (acceptedToleranceHours:float, webhookSecret:string, response_content: string, svix_id:string, svix_timestamp:string, svix_signature:string) =

        let isOk, timestampInt = Int64.TryParse svix_timestamp
        if not isOk then failwith $"Webook response: Incorrect timestamp header format {svix_id}"
        else
        let timestamp = DateTimeOffset.FromUnixTimeSeconds timestampInt
        if (DateTimeOffset.UtcNow - timestamp).TotalHours > acceptedToleranceHours then
            failwith $"Webook response: Too old response time {svix_id}"
        else

        let content = $"{svix_id}.{svix_timestamp}.{response_content}" |> System.Text.Encoding.UTF8.GetBytes
        let secretBytes = webhookSecret.Split('_').[1] |> Convert.FromBase64String

        use hmacsha256 = new System.Security.Cryptography.HMACSHA256(secretBytes)
        
        let hash = hmacsha256.ComputeHash content

        let base64Signature = Convert.ToBase64String hash

        if not (svix_signature.Contains base64Signature) then
            failwith $"Webhook response: Incorrect signature {svix_signature} in {svix_id}"
        else
        let r = JsonData.WebhookResponseRaw.Load(Serializer.Deserialize response_content) : CarmelWebhookResponse
        r
