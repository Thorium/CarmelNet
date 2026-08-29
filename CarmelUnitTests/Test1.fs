namespace CarmelUnitTests

open System
open CarmelNet
open Microsoft.VisualStudio.TestTools.UnitTesting

[<TestClass>]
type Test1() =

    // You will get clientId and Secret from Carmel
    let clientId = ""
    let clientSecret = ""
    // This is just id from URL of https://webhook.site/
    let webhookSiteGuid = ""

    let webhookTestEndpoint = "https://webhook.site/" + webhookSiteGuid
    let rnd = System.Random()

    // This variable is populated by CreateCreditTransferTest
    // and then used by FetchCreditTransferTest
    static let mutable testPaymentOrderId = Guid.Empty

    [<TestMethod>]
    member this.AuthTest() =
        async {
            let! access_token = CarmelPayment.getAcccessToken (CarmelEnvironment.Sandbox, clientId, clientSecret)

            Assert.IsTrue(access_token.ToString() <> """{"error":"invalid_request"}""")

        }
        |> Async.RunSynchronously


    [<TestMethod>]
    member this.GetOriginationAccountTest() =
        async {
            let! access_token = CarmelPayment.getAcccessToken (CarmelEnvironment.Sandbox, clientId, clientSecret)
            let! origAcc = CarmelPayment.getOriginationAccounts (CarmelEnvironment.Sandbox, access_token)

            //Assert.IsTrue(origAcc.OriginationAccounts.Length > 0);

            let accs =
                match origAcc with
                | Ok acc -> acc
                | Error(err, x) -> failwith (err.ToString())

            Assert.IsTrue(accs.Length > 0)

        }
        |> Async.RunSynchronously

    [<TestMethod>]
    member this.GetWebhooksTest() =
        async {

            let! access_token = CarmelPayment.getAcccessToken (CarmelEnvironment.Sandbox, clientId, clientSecret)


            match! CarmelWebhooks.getWebhookSubscriptions (CarmelEnvironment.Sandbox, access_token) with
            | Ok xs ->
                Assert.IsNotNull xs
                Assert.IsNotNull xs.Subscribers
                if xs.Subscribers.Length = 0 then
                    printfn "No subscriptions found"

                xs.Subscribers |> Seq.iter(fun x ->
                    if isNull x.EndpointUrl then
                        printfn "Null endpoint found"
                    else
                        printfn "Subscription found: %s status %s id %s" x.EndpointUrl x.Status (match x.Id with | Some y -> y.ToString() | None -> "None")
                )
            | Error(err, txt) ->
                printfn "Error getting webhooks: %s" txt
                raise err

        }
        |> Async.RunSynchronously

    /// Note: This test might add some data to the Sandbox environment.
    [<TestMethod>]
    member this.RegisterAndRemoveWebHook() =
        async {
            let webhooks = CarmelWebhooks.webhookEvents

            let! access_token = CarmelPayment.getAcccessToken (CarmelEnvironment.Sandbox, clientId, clientSecret)

            let! subscribed =
                CarmelWebhooks.createWebhookSubscription (CarmelEnvironment.Sandbox, access_token, webhookTestEndpoint, webhooks)

            let subscriberId =
                match subscribed with
                | Ok x ->
                    Assert.IsTrue(x.Subscriber.Id.IsSome, "Webhook subscription not found")
                    Assert.AreEqual<String>("active", x.Subscriber.Status)
                    x.Subscriber.Id.Value
                | Error(err, txt) ->
                    printfn "Error subscribing webhook: %s" txt
                    raise err

            let! webhookSecret =
                CarmelWebhooks.getWebhookSubscriptionSecret (CarmelEnvironment.Sandbox, access_token, subscriberId)

            Assert.IsFalse(String.IsNullOrEmpty webhookSecret, "Webhook secret not found")

            printfn "Webhook %O wired to: %s with secret %s" subscriberId webhookTestEndpoint webhookSecret

            if webhookTestEndpoint.StartsWith "https://webhook.site/" then
                printfn "Watch webhooks at https://webhook.site/#!/view/%O" webhookSiteGuid

            let! deleted =
                CarmelWebhooks.deleteWebhookSubscription (CarmelEnvironment.Sandbox, access_token, subscriberId)

            let delResp =
                match deleted with
                | Ok x ->
                    Assert.IsFalse(String.IsNullOrEmpty(x.ToString()), "Webhook subscription not found")
                    x
                | Error(err, txt) ->
                    printfn "Error deleting webhook: %s" txt
                    raise err

            return ()
        }
        |> Async.RunSynchronously

    /// Note: This test might add some data to the Sandbox environment.
    [<TestMethod>]
    member this.CreateCreditTransferTest() =
        async {

            // With different destination accounts you can cause different processes (and webhooks) to be tested.
            // https://docs.carmelsolutions.com/reference/introduction-1
            let destinationAccount =
                // "R01"            // ACH Return Simulation
                // "C01"            // ACH Correction
                // "uncorrected"    // ACH Failure
                // "remit"          // Remittance Simulations: remit (accept money transfer)
                // "remitreturn"    // Remittance Simulations: remit then return
                // "returnremit"    // Remittance Simulations: return then remit
                "1234567"

            let paymentCorrelationId = rnd.NextInt64(100_000, 999_999).ToString()

            let! access_token = CarmelPayment.getAcccessToken (CarmelEnvironment.Sandbox, clientId, clientSecret)
            let! origAccs = CarmelPayment.getOriginationAccounts (CarmelEnvironment.Sandbox, access_token)

            let paymentAccIc =
                match origAccs with
                | Ok acc ->
                    let firstAcc = (acc |> Seq.head)
                    firstAcc.Id.Value
                | Error(err, txt) ->
                    printfn "Error fetching orig account: %s" txt
                    raise err

            let! creditTransfer =
                CarmelPayment.createCreditTransfer (
                    CarmelEnvironment.Sandbox,
                    access_token,
                    paymentAccIc,
                    paymentCorrelationId,
                    100,
                    CarmelTransferType.ACH CarmelSecCode.PPD,
                    CarmelTransferDirection.Debit,
                    "091809524",
                    destinationAccount,
                    "Tuomas Testing",
                    CarmelBankAccountType.Checking,
                    None
                )

            match creditTransfer with
            | Error(err, txt) ->
                printfn "Error making credit transfer: %s" txt
                raise err
            | Ok resp ->
                Assert.IsTrue(resp.Id.IsSome, "Response Id not found")
                // Debit, ACH WEB: "approvalRequired"
                // Credit, ACH PPD: "approvalRequired"
                // Debit, ACH PPD: "approvalRequired"

                Assert.AreEqual<String>("approvalRequired", resp.Status)
                let paymentOrderId = resp.Id.Value
                testPaymentOrderId <- paymentOrderId

                let! approval =
                    CarmelPayment.approveCreditTransfer (
                        CarmelEnvironment.Sandbox,
                        access_token,
                        paymentOrderId,
                        CarmelApprovalAction.Approve
                    )

                match approval with
                | Error(err, txt) ->
                    printfn "Error approving credit transfer: %s" txt
                    raise err
                | Ok res2 ->
                    Assert.IsTrue(res2.Id.IsSome, "Response Id not found")
                    Assert.AreEqual<String>("approved", res2.Status)

                ()

        }
        |> Async.RunSynchronously


    [<TestMethod>]
    member this.FetchCreditTransfersTest() =
        async {

            let! access_token = CarmelPayment.getAcccessToken (CarmelEnvironment.Sandbox, clientId, clientSecret)

            let pageId = 1

            let! creditTransfer =
                CarmelPayment.fetchCreditTransfers (CarmelEnvironment.Sandbox, access_token, pageId, None, None, None)

            match creditTransfer with
            | Error(err, txt) ->
                printfn "Error fetching credit transfers: %s" txt
                raise err
            | Ok resp ->
                Assert.IsNotNull(resp, "Response not found")

                ()

        }
        |> Async.RunSynchronously

    [<TestMethod>]
    member this.FetchCreditTransferTest() =
        async {

            let! access_token = CarmelPayment.getAcccessToken (CarmelEnvironment.Sandbox, clientId, clientSecret)

            let paymentOrderId = testPaymentOrderId

            let! creditTransfer =
                CarmelPayment.fetchCreditTransfer (CarmelEnvironment.Sandbox, access_token, paymentOrderId)

            match creditTransfer with
            | Error(err, txt) ->
                if txt <> "One or more errors occurred. (Not Found)" then
                    printfn "Error fetching credit transfers: %s" txt
                    raise err
                else ()
            | Ok resp ->
                Assert.IsNotNull(resp, "Response not found")

                ()

        }
        |> Async.RunSynchronously

    [<TestMethod>]
    member this.FetchEventsTest() =
        async {

            let! access_token = CarmelPayment.getAcccessToken (CarmelEnvironment.Sandbox, clientId, clientSecret)

            let pageId = 1

            let! creditTransfer =
                CarmelPayment.fetchEvents (CarmelEnvironment.Sandbox, access_token, pageId, None, None, None)

            match creditTransfer with
            | Error(err, txt) ->
                printfn "Error fetching events: %s" txt
                raise err
            | Ok resp ->
                Assert.IsNotNull(resp, "Response not found")
                
                resp.Results |> Seq.iter(fun e ->
                    printfn $"Event: {e.Type} for payment {e.PaymentOrder} at {e.DateCreated}"
                )
        }
        |> Async.RunSynchronously

    [<TestMethod>]
    member this.WebhookParsingTest() =
        async {

            // Get this from getWebhookSubscriptionSecret
            let webhookSecret = "whsec_MfKQ9r8GKYqrTwjUPD8ILPZIo2LaLaSw"
            // Header: svix-id
            let svix_id = "msg_p5jXN8AQM9LWM0D4loKWxJek"
            // Header: svix-timestamp
            let svix_timestamp = "1614265330"
            // Header: svix-signature
            let svix_signature = "v1,g0hM9SsE+OTPJTGt/tmIKtSyZlE3uFJELVlNIOLJ1OE="
            // the raw content of the POST request
            let payload = "{\"test\": 2432232314}"

            // Tolerance hours to accept expiration. This is just an unit test value.
            // In real environtment use a figure like 0.5 (after checking possible timezone affections).
            let acceptedToleranceHours = 300_000.0 // 0.5

            let webhookResponse = CarmelWebhooks.parseWebhookResponse(acceptedToleranceHours, webhookSecret, payload, svix_id, svix_timestamp, svix_signature)

            Assert.IsNotNull(webhookResponse, "Response parsing failed")

            // Main fields will be:
            // webhookResponse.DateCreated
            // webhookResponse.Description
            // webhookResponse.Id
            // webhookResponse.PaymentOrder.Id
            // webhookResponse.Type

            return ()

        }
        |> Async.RunSynchronously
