---
description: Publish the Web and Server applications
---

To publish the applications for deployment to the server, run the following commands:

# Publish Web App (includes SMTP Server)
// turbo
dotnet publish ExchangeMail.Web/ExchangeMail.Web.csproj -c Release -o ./Publish/Web

# Publish Server Service (Background Worker)
// turbo
dotnet publish ExchangeMail.Server/ExchangeMail.Server.csproj -c Release -o ./Publish/Server
