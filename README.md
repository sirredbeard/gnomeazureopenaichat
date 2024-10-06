# gnomeazureopenaichat

Like [gnomephichat](https://github.com/sirredbeard/gnomephichat) but for chatting with Azure OpenAI instead of a local model.

Uses:

* .NET 9 RC
* GTK

Requires:

* An Azure OpenAI deployment. Configure the endpoint URI, API key, deployment name in the 'info' panel.

To run from source:

```
git clone https://github.com/sirredbeard/gnomeazureopenaichat
cd gnomeazureopenaichat
dotnet restore
dotnet run
```