using System.Collections.Generic;
using UE.CoreUObject;
using UE.Engine;
using UE.MovieRenderPipelineCore;

namespace NeoRune
{
    /// <summary>
    /// The handler of <see cref="Http.Responded"/>: a method taking the <see cref="HttpResponse"/>. (Mapped to an Engine event
    /// type with one object parameter, so subscribing doesn't make the mod depend on the HTTP plugin.)
    /// </summary>
    [UDelegate("/Script/Engine.OnPrimaryAssetLoaded__DelegateSignature")]
    public delegate void HttpEvent(HttpResponse response);

    /// <summary>A response to an <see cref="Http"/> request.</summary>
    public class HttpResponse : UObject
    {
        /// <summary>The number Get/Post/Send returned for the request.</summary>
        public int Request;
        /// <summary>The HTTP status: 200 = OK.</summary>
        public int Status;
        public string Body;
    }

    /// <summary>
    /// Web requests. The game has no HTTP functions of its own for mods; this uses the request sender of Unreal's Movie
    /// Render Pipeline plugin, which ships with the game. Keep one in a field:
    /// <code>
    /// http = Http.Create(this);
    /// if (http != null) { http.Responded += OnResponse; http.Get("https://example.com/data.json"); }
    /// ...
    /// void OnResponse(HttpResponse response) { if (response.Status == 200) Log.Write(response.Body); }
    /// </code>
    /// Responses arrive later, on the game thread. Safe without the plugin: everything that touches it is in a separate
    /// class loaded by path when Create runs, so if a game version lacked the plugin, Create returns null and the rest
    /// of the mod still loads.
    /// </summary>
    [CompileWith("HttpSender")]
    public class Http : UObject
    {
        public event HttpEvent Responded;
        HttpSenderBase? sender;

        /// <summary>A request sender, or null when this game version can't send web requests.</summary>
        public static Http? Create(UObject owner)
        {
            var http = UGameplayStatics.SpawnObject(Unreal.ClassOf<Http>(), owner) as Http;
            return http != null && http.Init() ? http : null;
        }

        /// <summary>Sends a GET request; returns its number (or -1 when it couldn't be sent).</summary>
        public int Get(string url) => Send(url, "GET", "", new Dictionary<string, string>());

        /// <summary>Sends a POST request with a body, e.g. contentType "application/json"; returns its number (or -1).</summary>
        public int Post(string url, string body, string contentType)
        {
            var headers = new Dictionary<string, string>();
            headers["Content-Type"] = contentType;
            return Send(url, "POST", body, headers);
        }

        /// <summary>Any verb with your own headers; returns the request number (or -1).</summary>
        public int Send(string url, string verb, string body, Dictionary<string, string> headers)
        {
            if (sender == null) return -1;
            return sender.Send(url, verb, body, headers);
        }

        /// <summary>Called by the sender when a response arrives.</summary>
        public void Deliver(int request, int status, string body)
        {
            var response = UGameplayStatics.SpawnObject(Unreal.ClassOf<HttpResponse>(), this) as HttpResponse;
            if (response == null) return;
            response.Request = request;
            response.Status = status;
            response.Body = body;
            Responded(response);
        }

        bool Init()
        {
            // By path, not by type: Http itself must not depend on the plugin.
            var senderClass = Unreal.LoadClass<HttpSenderBase>($"/Game/Mods/{Unreal.ModName}/HttpSender.HttpSender_C");
            if (senderClass == null) return false;
            sender = UGameplayStatics.SpawnObject(senderClass, this) as HttpSenderBase;
            return sender != null && sender.Start(this);
        }
    }

    /// <summary>What Http calls on its sender. Abstract, so the calls go by name and Http needs no import of HttpSender.</summary>
    public abstract class HttpSenderBase : UObject
    {
        public abstract bool Start(Http owner);
        public abstract int Send(string url, string verb, string body, Dictionary<string, string> headers);
    }

    /// <summary>The part of Http that uses the Movie Render Pipeline plugin. Only loaded by path (see Http.Init).</summary>
    public class HttpSender : HttpSenderBase
    {
        UMoviePipelineInProcessExecutor? executor;
        Http? owner;

        public override bool Start(Http http)
        {
            owner = http;
            executor = UGameplayStatics.SpawnObject(Unreal.ClassOf<UMoviePipelineInProcessExecutor>(), this) as UMoviePipelineInProcessExecutor;
            if (executor == null) return false;
            executor.HTTPResponseRecievedDelegate += OnResponse;
            return true;
        }

        public override int Send(string url, string verb, string body, Dictionary<string, string> headers)
        {
            if (executor == null) return -1;
            return executor.SendHTTPRequest(url, verb, body, headers);
        }

        void OnResponse(int request, int status, string body) => owner?.Deliver(request, status, body);
    }
}
