[<RequireQualifiedAccess>]
module JsonData

// The commented types here are, because the Carmel Open API schema file doesn't always 100% correspond to the actual implementation.
// That's why there is a workaround possibility to use FSharp.Data instead of SwaggerProvider.

//type OriginationAccountsResponse = FSharp.Data.JsonProvider<"""[{
//    "originationAccounts": [
//        {
//            "accountNumber": "**0127",
//            "achEnabled": true,
//            "creditEnabled": false,
//            "debitEnabled": false,
//            "id": "27cd7c02-3bf4-4dd2-a0d7-0c4e3c8c91a9",
//            "name": "0127 - DEF CO Debit - FFCU",
//            "originationInstitution": {
//                "id": "a00ccc84-fc84-4068-9503-4962aa8dbfc3",
//                "name": "Fidelity Fiduciary Credit Union"
//            },
//            "outboundCompanyName": "DEF Co",
//            "routingNumber": "21000021",
//            "sameDayCreditEnabled": false,
//            "sameDayDebitEnabled": false,
//            "status": "inactive"
//        },
//        {
//            "accountNumber": "**0127",
//            "achEnabled": true,
//            "creditEnabled": false,
//            "debitEnabled": false,
//            "id": "cacc734d-e2a4-4a2a-8790-b197ccea5fcc",
//            "name": "0127 - DEF CO Credit - FFCU",
//            "originationInstitution": {
//                "id": "a00ccc84-fc84-4068-9503-4962aa8dbfc3",
//                "name": "Fidelity Fiduciary Credit Union"
//            },
//            "outboundCompanyName": "DEF Co",
//            "routingNumber": "21000021",
//            "sameDayCreditEnabled": false,
//            "sameDayDebitEnabled": false,
//            "status": "inactive"
//        },
//        {
//            "accountNumber": "**0128",
//            "achEnabled": true,
//            "creditEnabled": false,
//            "debitEnabled": false,
//            "id": "9ab7d7d4-e32f-43cb-8cdf-cc4d6fd9e28f",
//            "name": "0128 - DEF CO Debit - FFCU",
//            "originationInstitution": {
//                "id": "a00ccc84-fc84-4068-9503-4962aa8dbfc3",
//                "name": "Fidelity Fiduciary Credit Union"
//            },
//            "outboundCompanyName": "DEF Co",
//            "routingNumber": "21000021",
//            "sameDayCreditEnabled": false,
//            "sameDayDebitEnabled": false,
//            "status": "inactive"
//        },
//        {
//            "accountNumber": "**0128",
//            "achEnabled": true,
//            "creditEnabled": false,
//            "debitEnabled": false,
//            "id": "89cff511-a6f9-4ca8-813d-b35ae4ed4c4e",
//            "name": "0128 - DEF CO Credit - FFCU",
//            "originationInstitution": {
//                "id": "a00ccc84-fc84-4068-9503-4962aa8dbfc3",
//                "name": "Fidelity Fiduciary Credit Union"
//            },
//            "outboundCompanyName": "DEF Co",
//            "routingNumber": "21000021",
//            "sameDayCreditEnabled": false,
//            "sameDayDebitEnabled": false,
//            "status": "inactive"
//        },
//        {
//            "accountNumber": "**0129",
//            "achEnabled": true,
//            "creditEnabled": true,
//            "debitEnabled": true,
//            "id": "ab13be13-4036-4f9b-bffc-411e365a7d8b",
//            "name": "0129 - DEF CODebit - IMPB",
//            "originationInstitution": {
//                "id": "45b27748-a107-4d88-b47f-722795e0112d",
//                "name": "Imperial Bank"
//            },
//            "outboundCompanyName": "DEF Co",
//            "routingNumber": "91000019",
//            "sameDayCreditEnabled": false,
//            "sameDayDebitEnabled": false,
//            "status": "active"
//        },
//        {
//            "accountNumber": "**0129",
//            "achEnabled": true,
//            "creditEnabled": true,
//            "debitEnabled": true,
//            "id": "136efacf-3509-4262-aac8-817687618999",
//            "name": "0129 - DEF CO Credit - IMPB",
//            "originationInstitution": {
//                "id": "45b27748-a107-4d88-b47f-722795e0112d",
//                "name": "Imperial Bank"
//            },
//            "outboundCompanyName": "DEF Co",
//            "routingNumber": "91000019",
//            "sameDayCreditEnabled": false,
//            "sameDayDebitEnabled": false,
//            "status": "active"
//        },
//        {
//            "accountNumber": "**0130",
//            "achEnabled": true,
//            "creditEnabled": true,
//            "debitEnabled": true,
//            "id": "94e66a0b-7b45-41bf-8cbc-f6cbd2d59de5",
//            "name": "0130 - DEF CO Debit - IMPB",
//            "originationInstitution": {
//                "id": "45b27748-a107-4d88-b47f-722795e0112d",
//                "name": "Imperial Bank"
//            },
//            "outboundCompanyName": "DEF Co",
//            "routingNumber": "91000019",
//            "sameDayCreditEnabled": false,
//            "sameDayDebitEnabled": false,
//            "status": "active"
//        },
//        {
//            "accountNumber": "**0130",
//            "achEnabled": true,
//            "creditEnabled": true,
//            "debitEnabled": true,
//            "id": "91d18b26-572b-40c3-bcba-39ed345a224d",
//            "name": "0130 - DEF CO Credit - IMPB",
//            "originationInstitution": {
//                "id": "45b27748-a107-4d88-b47f-722795e0112d",
//                "name": "Imperial Bank"
//            },
//            "outboundCompanyName": "DEF Co",
//            "routingNumber": "91000019",
//            "sameDayCreditEnabled": false,
//            "sameDayDebitEnabled": false,
//            "status": "active"
//        },    {
//          "accountNumber": "string",
//          "achEnabled": true,
//          "achCreditEnabled": true,
//          "achDebitEnabled": true,
//          "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
//          "name": "string",
//          "originationInstitution": {
//            "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
//            "name": "string"
//          },
//          "outboundCompanyName": "string",
//          "routingNumber": "string",
//          "sameDayCreditEnabled": true,
//          "sameDayDebitEnabled": true,
//          "status": "string",
//          "supplementalDataKeys": [
//            "string"
//          ],
//          "wireEnabled": true
//        }
//    ]
//},{
//  "type": "string",
//  "title": "string",
//  "status": 0,
//  "detail": "string",
//  "instance": "string"
//}]""", SampleIsList=true>


//type PaymentOrderRequest =
//    FSharp.Data.JsonProvider<"""[{
//          "type": "ACH",
//          "direction": "debit",
//          "receivingAccount": {
//            "accountNumber": "asdf123",
//            "name": "mr test",
//            "routingNumber": "943012484"
//          },
//          "amount": 100,
//          "effectiveDate": "2024-01-01",
//          "originationAccountId": "asdf"
//        },{
//            "amount":12599,
//            "direction":"debit",
//            "effectiveDate": "2021-05-27",
//            "originationAccountId": "ab13be13-4036-4f9b-bffc-411e365a7d8b",
//            "receivingAccount": {
//                "accountNumber": "00-1234567",
//                "name": "Joe Customer",
//                "routingNumber": "074903719",
//                "type": "checking"
//            },  
//            "subType": "WEB",
//            "type": "ach"
//        },{
//            "amount":400000,
//            "direction":"credit",
//            "effectiveDate": "2021-05-27",
//            "originationAccountId": "ab13be13-4036-4f9b-bffc-411e365a7d8b",
//            "receivingAccount": {
//                "accountNumber": "00-1234567",
//                "name": "Joe Customer",
//                "address1": "15442 Smith Street",
//                "address2": "Apartment 44",
//                "address3": "Reno, NV 89443",
//                "routingNumber": "074903719"
//            },
//            "type": "wire"
//        },{
//           "amount":12599,
//           "direction":"debit",
//           "effectiveDate":"2021-05-27",
//           "originationAccountId":"27cd7c02-3bf4-4dd2-a0d7-0c4e3c8c91a9",
//           "receivingAccount":{
//              "accountNumber":"00-1234567",
//              "name":"Joe Customer",
//              "routingNumber":"074903719",
//              "type":"checking"
//           },
//           "subType":"WEB",
//           "supplementalData":{
//              "clientIdCode":"0014422",
//              "processGroup":"2021-green"
//           },
//           "type":"ach"
//        },{
//            "amount":12599,
//            "direction":"debit",
//            "effectiveDate": "2023-08-29",
//            "metadata": {
//                "frequency": "recurring",
//                "payment_id": 5112,
//                "program_type": "simple"
//            },
//            "originationAccountId": "023e5dee-3795-427d-9c2c-0e97e6199f96",
//            "receivingAccount": {
//                "accountNumber": "00-1234567",
//                "name": "Joe Customer",
//                "routingNumber": "074903719",
//                "type": "checking"
//            },
//            "subType": "WEB",
//            "type": "ACH"
//        }]""", SampleIsList=true>

//type PaymentOrderResponse =
//    FSharp.Data.JsonProvider<"""[{
//            "paymentOrder": {
//                "amount": 12599,
//                "dateCreated": "2021-05-27T22:11:23.6399240Z",
//                "direction": "debit",
//                "effectiveDate": "2021-05-27",
//                "id": "19e82811-1f2d-4ce2-2eec-08d9213a29a5",
//                "originationAccountId": "ab13be13-4036-4f9b-bffc-411e365a7d8b",
//                "receivingAccount": {
//                    "accountNumber": "00-1234567",
//                    "name": "Joe Customer",
//                    "routingNumber": "074903719",
//                    "type": "checking"
//                },
//                "status": "approvalRequired",
//                "subType": "WEB",
//                "type": "ACH"
//            }
//        },{
//            "paymentOrder": {
//                "amount": 400000,
//                "dateCreated": "2021-05-27T22:11:25.6399240Z",
//                "direction": "credit",
//                "effectiveDate": "2021-05-27",
//                "id": "99988811-1f2d-4ce2-2eec-08d9213a29a5",
//                "originationAccountId": "ab13be13-4036-4f9b-bffc-411e365a7d8b",
//                "receivingAccount": {
//                    "accountNumber": "00-1234567",
//                    "name": "Joe Customer",
//                    "address1": "15442 Smith Street",
//                    "address2": "Apartment 44",
//                    "address3": "Reno, NV 89443",
//                    "routingNumber": "074903719"
//                },
//                "status": "approvalRequired",
//                "type": "wire"
//            }
//        },{
//            "paymentOrder": {
//                "amount": 12599,
//                "dateCreated": "2023-08-29T21:03:59.2000000Z",
//                "direction": "debit",
//                "effectiveDate": "2023-08-29",
//                "id": "19e82811-1f2d-4ce2-2eec-08d9213a29a5",
//                "metadata": {
//                    "billing_frequency": "recurring",
//                    "loan_id": "LOAN-A5",
//                    "payment_id": 5112,
//                    "program_type": "simple-plus"
//                },
//                "originationAccountId": "023e5dee-3795-427d-9c2c-0e97e6199f96",
//                "receivingAccount": {
//                    "accountNumber": "*567",
//                    "name": "Joe Customer",
//                    "routingNumber": "074903719",
//                    "type": "checking"
//                },
//                "status": "approvalRequired",
//                "subType": "WEB",
//                "type": "ACH"
//            }
//        },{
//          "paymentOrder": {
//            "amount": 0,
//            "corrections": [
//              {
//                "code": "C01",
//                "correctedData": {
//                  "accountNumber": "string",
//                  "routingNumber": "string",
//                  "transactionCode": "string"
//                },
//                "dateCorrected": "string",
//                "description": "string"
//              }
//            ],
//            "dateCreated": "string",
//            "dateModified": "string",
//            "direction": "debit",
//            "effectiveDate": "string",
//            "failure": {
//              "failureData": {
//                "priorPaymentOrderId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
//              },
//              "dateFailed": "string",
//              "description": "string"
//            },
//            "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
//            "originationAccountId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
//            "processingData": {
//              "runName": "string",
//              "traceNumber": "string"
//            },
//            "receivingAccount": {
//              "accountNumber": "string",
//              "address1": "string",
//              "address2": "string",
//              "address3": "string",
//              "finalCreditInfo1": "string",
//              "finalCreditInfo2": "string",
//              "finalCreditInfo3": "string",
//              "finalCreditInfo4": "string",
//              "name": "string",
//              "routingNumber": "string",
//              "type": "checking"
//            },
//            "remittanceHistory": [
//              {
//                "amount": 0,
//                "effectiveDate": "string",
//                "processedDate": "string"
//              }
//            ],
//            "return": {
//              "amount": 0,
//              "code": "string",
//              "dateReturned": "string",
//              "description": "string"
//            },
//            "status": "approvalRequired",
//            "subType": "PPD",
//            "supplementalData": {
//              "additionalProp": "string"
//            },
//            "type": "ACH"
//          }
//        },{
//              "type": "string",
//              "title": "string",
//              "status": 0,
//              "detail": "string",
//              "instance": "string"
//        },{
//          "errors": [
//            {
//              "message": "string",
//              "detail": "string"
//            }
//          ]
//        },{
//          "statusCode": 0
//        }]""", SampleIsList=true>

//type PaymentOrderApprovalRequest =
//    FSharp.Data.JsonProvider<"""[{
//        "value": "approved",
//        "path": "/status",
//        "op": "replace"
//    },{
//        "value": "remove",
//        "path": "/status"
//    }]""", SampleIsList=true>

type internal WebhookResponseRaw =
    FSharp.Data.JsonProvider<"""[{
          "dateCreated": "2022-12-29T22:30:04.1700000Z",
          "description": "Payment Order requires approval.",
          "id": "91e6f072-6747-4dbe-b907-b4f242714c6d",
          "paymentOrder": {
            "id": "9ab86b30-84c7-40e7-263c-08dae9ec3b12"
          },
          "type": "approvalRequired"
        },{
          "dateCreated": "2022-12-29T22:30:16.1370000Z",
          "description": "Payment Order has been approved.",
          "id": "e46aa832-2476-4cac-a049-d4be352c0fad",
          "paymentOrder": {
            "id": "9ab86b30-84c7-40e7-263c-08dae9ec3b12"
          },
          "type": "approved"
        },{
          "dateCreated": "2022-12-29T22:49:46.9770000Z",
          "description": "Payment Order has been sent.",
          "id": "8d1b4b80-8631-4c20-a49b-6c24e312574b",
          "paymentOrder": {
            "id": "9ab86b30-84c7-40e7-263c-08dae9ec3b12"
          },
          "type": "sent"
        },{
          "id": "8adfae88-9b9e-403e-8397-f5ea938de756",
          "dateCreated": "2023-01-03T21:01:13.0400000Z",
          "description": "Payment Order has been returned.",
          "paymentOrder": {
            "id": "9ab86b30-84c7-40e7-263c-08dae9ec3b12"
          },
          "return": {
            "amount": 32834,
            "code": "R01",
            "dateReturned": "2021-04-23",
            "description": "Insufficient Funds"
          },
          "type": "returned"
        },{
          "correction": {
            "code": "C01",
            "correctedData": {
              "accountNumber": "123"
            },
            "dateCorrected": "2023-01-02T18:44:15.3800000Z",
            "description": "Incorrect DFI Account Number"
          },
          "dateCreated": "2023-01-03T12:34:15.3870000Z",
          "description": "Payment Order has been corrected.",
          "id": "0adb8f6a-fbe1-4a0d-8458-e38df1a74bdd",
          "paymentOrder": {
            "id": "9ab86b30-84c7-40e7-263c-08dae9ec3b12"
          },
          "type": "corrected"
        },{
          "correction": {
            "code": "C02",
            "correctedData": {
              "routingNumber": "021000021"
            },
            "dateCorrected": "2024-04-10T15:47:11.2300000Z",
            "description": "Incorrect Routing Number"
          },
          "dateCreated": "2024-04-10T15:47:11.6730000Z",
          "description": "Payment Order has been corrected.",
          "id": "182a65c3-4968-4aeb-ae7c-a8f74fb8cd5c",
          "paymentOrder": {
            "id": "3e556ce7-7de0-43e7-8cc2-2d3dec4411c4"
          },
          "type": "corrected"
        },{
          "correction": {
            "code": "C03",
            "correctedData": {
              "accountNumber": "3941222237",
              "routingNumber": "107005076"
            },
            "dateCorrected": "2024-04-10T15:47:11.2300000Z",
            "description": "Incorrect Routing Number and Incorrect DFI Account Number"
          },
          "dateCreated": "2024-04-10T15:47:11.6730000Z",
          "description": "Payment Order has been corrected.",
          "id": "08150d13-09c8-491e-b22c-90e680161c5d",
          "paymentOrder": {
            "id": "2fb83c2a-4a63-41ce-9894-6a0f0fbb3f49"
          },
          "type": "corrected"
        },{
          "correction": {
            "code": "C05",
            "correctedData": {
              "transactionCode": "37"
            },
            "dateCorrected": "2024-04-10T15:47:11.2300000Z",
            "description": "Incorrect Transaction Code"
          },
          "dateCreated": "2024-04-10T15:47:11.6730000Z",
          "description": "Payment Order has been corrected.",
          "id": "9370a9f2-a376-4f00-8e9f-c8fedd91365e",
          "paymentOrder": {
            "id": "d8299619-8990-4c0e-837e-456e00b4a236"
          },
          "type": "corrected"
        },{
          "correction": {
            "code": "C06",
            "correctedData": {
              "accountNumber": "1000091331506",
              "transactionCode": "37"
            },
            "dateCorrected": "2024-04-10T15:47:11.2300000Z",
            "description": "Incorrect DFI Account Number and Incorrect Transaction Code"
          },
          "dateCreated": "2024-04-10T15:47:11.6730000Z",
          "description": "Payment Order has been corrected.",
          "id": "3609e51a-5e0e-4de3-b14b-733a2956c344",
          "paymentOrder": {
            "id": "b703510c-150c-49ff-93f5-4413a12a2b30"
          },
          "type": "corrected"
        },{
          "correction": {
            "code": "C07",
            "correctedData": {
              "accountNumber": "0005159789599",
              "routingNumber": "055003337",
              "transactionCode": "22"
            },
            "dateCorrected": "2024-04-10T15:47:11.2300000Z",
            "description": "Incorrect Routing Number, Incorrect DFI Account Number, and Incorrect Transaction Code"
          },
          "dateCreated": "2024-04-10T15:47:11.6730000Z",
          "description": "Payment Order has been corrected.",
          "id": "037eea5d-a8be-452a-a703-a30f06863b89",
          "paymentOrder": {
            "id": "79d705d4-8fbe-486d-9adc-988b79772e7d"
          },
          "type": "corrected"
        },{
          "dateCreated": "2023-01-04T18:38:56.3400000Z",
          "description": "Payment Order has been remitted.",
          "id": "d15ed92a-1ca4-47b3-87ce-ab3bd80a2ee3",
          "paymentOrder": {
            "id": "9ab86b30-84c7-40e7-263c-08dae9ec3b12"
          },
          "remittance": {
            "amount": 4500,
            "effectiveDate": "2023-01-06T00:00:00.0000000Z",
            "processedDate": "2023-01-05T16:41:41.4470000Z"
          },
          "type": "remitted"
        },{
          "dateCreated": "2023-01-02T18:50:32.5370000Z",
          "description": "Payment Order has been failed and will not be processed.",
          "failure": {
            "failureData": {
              "priorPaymentOrderId": "3574ea79-95be-4465-7797-08d97f8a20b7"
            },
            "dateFailed": "2023-01-02T18:50:09.4070000Z",
            "description": "Uncorrected NOC"
          },
          "id": "b8b5dd8c-7acb-4102-9025-6abd864f5430",
          "paymentOrder": {
            "id": "9ab86b30-84c7-40e7-263c-08dae9ec3b12"
          },
          "type": "failed"
        },{
          "dateCreated": "2022-12-30T17:27:34.1400000Z",
          "description": "Payment Order has been cancelled.",
          "id": "8329107f-83c3-433a-94ff-08b087d4b59f",
          "paymentOrder": {
            "id": "9ab86b30-84c7-40e7-263c-08dae9ec3b12"
          },
          "type": "cancelled"
        }]""", SampleIsList=true>

//type SimulationReturnCode =
//    FSharp.Data.JsonProvider<"""[{
//        "amount": 10000, 
//        "direction": "DEBIT",
//        "effectiveDate": "2022-03-25",
//        "originationAccountId": "6cdcee8e-7f13-4672-9e33-2ed615cd7b07",
//        "receivingAccount": {
//            "accountNumber": "R01",
//            "name": "Luke Skywalker",
//            "routingNumber": "074903719",
//            "type": "Checking"
//        },
//        "subType": "PPD",
//        "type": "ACH"
//    },{
//        "amount": 10000, 
//        "direction": "credit",
//        "effectiveDate": "2022-03-25",
//        "originationAccountId": "6cdcee8e-7f13-4672-9e33-2ed615cd7b07",
//        "receivingAccount": {
//            "accountNumber": "return",
//            "name": "Luke Skywalker",
//            "address1": "15442 Smith Street",
//            "address2": "Apartment 44",
//            "address3": "Reno, NV 89443",        
//            "routingNumber": "074903719"
//        },
//        "type": "Wire"
//    },{
//        "amount": 10000, 
//        "direction": "DEBIT",
//        "effectiveDate": "2022-03-25",
//        "originationAccountId": "6cdcee8e-7f13-4672-9e33-2ed615cd7b07",
//        "receivingAccount": {
//            "accountNumber": "C01",
//            "name": "Luke Skywalker",
//            "routingNumber": "074903719",
//            "type": "Checking"
//        },
//        "subType": "PPD",
//        "type": "ACH"
//    },{
//        "amount": 10000, 
//        "direction": "DEBIT",
//        "effectiveDate": "2022-03-25",
//        "originationAccountId": "6cdcee8e-7f13-4672-9e33-2ed615cd7b07",
//        "receivingAccount": {
//            "accountNumber": "uncorrected",
//            "name": "Luke Skywalker",
//            "routingNumber": "074903719",
//            "type": "Checking"
//        },
//        "subType": "PPD",
//        "type": "ACH"
//    },{
//        "amount": 10000, 
//        "direction": "credit",
//        "effectiveDate": "2022-03-25",
//        "originationAccountId": "6cdcee8e-7f13-4672-9e33-2ed615cd7b07",
//        "receivingAccount": {
//            "accountNumber": "uncorrected",
//            "name": "Luke Skywalker",
//            "address1": "15442 Smith Street",
//            "address2": "Apartment 44",
//            "address3": "Reno, NV 89443",        
//            "routingNumber": "074903719"
//        },
//        "type": "Wire"
//    },{
//        "amount": 10000, 
//        "direction": "DEBIT",
//        "effectiveDate": "2022-03-25",
//        "originationAccountId": "6cdcee8e-7f13-4672-9e33-2ed615cd7b07",
//        "receivingAccount": {
//            "accountNumber": "remit",
//            "name": "Luke Skywalker",
//            "routingNumber": "074903719",
//            "type": "Checking"
//        },
//        "subType": "PPD",
//        "type": "ACH"
//    },{
//        "amount": 10000, 
//        "direction": "DEBIT",
//        "effectiveDate": "2022-03-25",
//        "originationAccountId": "6cdcee8e-7f13-4672-9e33-2ed615cd7b07",
//        "receivingAccount": {
//            "accountNumber": "returnremit",
//            "name": "Luke Skywalker",
//            "routingNumber": "074903719",
//            "type": "Checking"
//        },
//        "subType": "PPD",
//        "type": "ACH"
//    },{
//        "amount": 10000, 
//        "direction": "DEBIT",
//        "effectiveDate": "2022-03-25",
//        "originationAccountId": "6cdcee8e-7f13-4672-9e33-2ed615cd7b07",
//        "receivingAccount": {
//            "accountNumber": "remitreturn",
//            "name": "Luke Skywalker",
//            "routingNumber": "074903719",
//            "type": "Checking"
//        },
//        "subType": "PPD",
//        "type": "ACH"
//    }]""", SampleIsList=true>

//type WebhookSubscriptionRequest = FSharp.Data.JsonProvider<"""[{
//      "endpointUrl": "http://webhook.com",
//      "events": [
//        "paymentOrder_approvalRequired",
//        "paymentOrder_approved",
//        "paymentOrder_cancelled",
//        "paymentOrder_corrected",
//        "paymentOrder_failed",
//        "paymentOrder_remitted",
//        "paymentOrder_remitted",
//        "paymentOrder_sent"
//      ]
//}]""", SampleIsList=true>

//type WebhookSubscriptionResponse = FSharp.Data.JsonProvider<"""[{
//  "subscriber": {
//    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
//    "endpointUrl": "string",
//    "version": 0,
//    "events": [
//      "paymentOrder_approvalRequired"
//    ],
//    "status": "active",
//    "dateCreated": "string"
//  }
//}]""", SampleIsList=true>

type internal WebhookSubscriptionSecretResponseRaw =
    FSharp.Data.JsonProvider<"""[{
        "secret": "whsec_123jeR21MNm1NW2d18W0m1exMP123/2u"
    }
    ]""", SampleIsList=true>


open SwaggerProvider
open System.Net.Http

/// Schema is just downloaded from docs.carmelsolutions.com/openapi/60aea6b71cc8feb5ff3bad79
[<Literal>]
let schemaCarmel = __SOURCE_DIRECTORY__ + @"/carmel-openapi-schema.json"

type CarmelOpenApi = OpenApiClientProvider<schemaCarmel, PreferAsync=true>

let unSuccessStatusCode = Event<_>() // id, status, content

type ErrorHandler(messageHandler) =
    inherit DelegatingHandler(messageHandler)

    override __.SendAsync(request, cancellationToken) =
        let resp = base.SendAsync(request, cancellationToken)

        async {
            let! result = resp |> Async.AwaitTask

            if not result.IsSuccessStatusCode then
                let! cont = result.Content.ReadAsStringAsync() |> Async.AwaitTask
                let hasId, idvals = request.Headers.TryGetValues "X-Request-ID" // Some unique id

                unSuccessStatusCode.Trigger(
                    (if not hasId then None else idvals |> Seq.tryHead),
                    result.StatusCode,
                    cont
                )

            return result
        }
        |> Async.StartImmediateAsTask

let internal reportUnsuccessfulEvents (* xRequestId *) handler =
    let evt =
        unSuccessStatusCode.Publish
        //|> Event.filter(fun (id,status,content) -> id = Some xRequestId)
        |> Event.map (fun (id, status, content) -> status, content)

    evt.Subscribe(fun (s, c) -> handler (s, c))
