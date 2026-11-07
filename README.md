Unofficial .NET client for [Carmel](https://www.carmelsolutions.com/), making bank payments in the US.

Complete API documentation: https://docs.carmelsolutions.com/


To get an access token, call: CarmelPayment.getAcccessToken

To register a new webhook, call: CarmelWebhooks.createWebhookSubscription

To get a list of origination accounts, call: CarmelPayment.getOriginationAccounts

Then to create payments, call: CarmelPayment.createCreditTransfer

And to approve the payments, call: CarmelPayment.approveCreditTransfer

To verify the webhook signatures and parse the response: CarmelWebhooks.parseWebhookResponse

When processing a webhook, fetch the details via: CarmelPayment.fetchCreditTransfer

See the unit tests for examples.

Printing a CarmelAccessToken (ToString, %A, string interpolation) hides it, so a header built that way sends "Bearer CarmelAccessToken hidden". To read the token, match the case: `let (CarmelAccessToken.Token raw) = token`.

Running a returned Async again sends the request again, and this library never retries by itself.

CarmelPayment.createCreditTransfer is safe to retry only while Carmel holds the key: the Idempotency-Key comes from paymentId and the effective date from the first run. Carmel may forget a key after 24 hours, and a later retry can then send a second ACH, so read the order's status with CarmelPayment.fetchCreditTransfers first. A new call with the same paymentId after midnight sends the same key with a new date. A blank paymentId throws and sends nothing.

CarmelWebhooks.createWebhookSubscription is safe to retry: the Idempotency-Key is derived from the endpoint and events. Carmel may forget a key after 24 hours. Until then, registering the same endpoint and events after deleting the listener gets the deleted listener's response: use CarmelWebhooks.createWebhookSubscriptionWithKey with a new id.

CarmelPayment.approveCreditTransfer is not safe to retry: Carmel's docs give Idempotency-Key to POST requests only and this is a PATCH. After a failure, read the status with CarmelPayment.fetchCreditTransfer first.

Nuget: https://www.nuget.org/packages/CarmelNet
