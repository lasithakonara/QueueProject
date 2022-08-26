# Support Request Handler Project Logic

- SupportReqeustAcceptor is the API layer which exposed to the chat clients or to the persons who wants to sent support requests
- SupportReqeustAcceptor will check the queue capacity and current queue size and decide whether or not to accept the new incoming requests
- One the support reqeust is accpeted, the client starts polling.
- SupportReqeustAcceptor has a background process to monitor these chat sessions which monitors whether they are still connected or not by using a polling mechanism
- If 3 polling requests fails, SupportReqeustAcceptor will mark the session as inactive and communicate the same to SupportReqeustProcessor.
- SupportReqeustProcessor will search for the inactive session in the agent assignments and if not found it will be added to a queue.
- SupportReqeustProcessor has a background process which always reads new requests from the RabbitMQ queue as and when agents become availble.
- Agents have 3 shifts and the system automatically handles the shift capacity as per the given logic.

# Major Technologies Used

- RabbitMQ
-.NET Core