## UnityPaste
#### Share logs and other text online from within Unity

It's very common to need to write text to disk from within Unity during development. You might have an editor mode that needs to save level information, or debugging code that needs to dump information to a log file. Writing a text file is easy enough to do, but accessing that file can be inconvenient, especially with mobile device builds. It'd also be handy to have all your log files aggregated in one place.

The UnityPaste component solves this problem by writing text not to a file on the device but to the [TinyPaste](http://tny.cz) web service. There are a number of competitors to TinyPaste--Pastebin being the oldest and most famous--but I found TinyPaste's API to be the easiest to call from a Unity context. The unique feature of "paste" services like TinyPaste and Pastebin is that they are anonymous and lightweight, relying on obscurity to provide privacy. When you paste text to these services, you get a cryptic URL back like [http://tny.cz/fc82a18e](http://tny.cz/fc82a18e). Lose that URL, and the text is lost for good! TinyPaste offers an authenticated mode that gives you a list on demand of all your pastes.

Note that this code will not work in a webplayer build, due to the cross-domain security model. (TinyPaste has no crossdomain.xml at their root.)

### Usage
  1.  Attach the TinyPaste component to an object in your scene. (If you're using a singleton framework such as [mine](http://stebeegizmo.github.io/Singleton/), TinyPaste is an excellent candidate for turning into a singleton instance.)
  2.  If desired, fill in your TinyPaste username and password in the object's fields. As discussed above, you don't have to do this, but if you don't, the only way you'll find your pasted files again is by capturing their URLs returned from the service.
  3.  Call `Create(body, title, callback)`, where `body` is the text to paste and `title` is an optional title for the pasted page. The `callback` value can either be a `GameObject` or a delegate method:
      * If you pass in a `GameObject`, then when the request is complete the component will call `SendMessage` on your object, calling the `OnTinyPasteSucceeded` method if successful or `OnTinyPasteFailed` if not. You can override these by passing in your own function names after the reference to your object--for example, if you want the methods to be `PasteGood` and `PasteBad` instead then call `Create(body, title, yourObject, "PasteGood", "PasteBad")`.
      * You can pass in an instance of the `TinyPasteResponse(string response, string error)` delegate.
      * You can use a lambda to pass in an anonymous handler:
        `Create(body, title, (response, error) => { if (string.IsNullOrEmpty(error)) Debug.Log(response); }`
      * Or you can choose not to provide a callback at all, in which case the request is "fire and forget"--you won't know whether it succeeded. This only makes sense if you've provided a username and password, since otherwise you won't have any way to go back and find the data you pasted.
  4.  On success, your callback receives the response from TinyPaste. The format will be JSON, but you can request XML instead by setting the component's `ResponseFormat` property. *It is up to you to parse the response,* because I didn't want to pre-judge what JSON parsing package you're using.

#### Example
This example assumes you're using Matt Schoen's [JSONObject](http://wiki.unity3d.com/index.php?title=JSONObject) parser.
```csharp
public TinyPaste TPComponent;

void onPasteComplete(string response, string error)
{
  if (!string.IsNullOrEmpty(error))
    Debug.LogWarning(error);
  else
  {
  		JSONObject json = new JSONObject(response);
  		// Response will look like {"result":{"response":"fc82a18e"}}

  		string id = json["result"]["response"].str;
  		Debug.Log("Your paste can be found at http://tny.cz/" + id);
  }
}

void doPasteTest()
{
  TPComponent.Username = "MyUsername";
  TPComponent.Password = "12345isthecombinationonmyluggage";
  TPComponent.Create("This is a test paste", "Test Paste", onPasteComplete);
}
```

#### Other Methods
The component provides simple access to the rest of the TinyPaste API as well:
  * `Get(id, callback)`: Fetch the paste with the specified ID.
  * `List(callback)`: Fetch a list of all your pastes. Requires username and password to be set.
  * `Delete(id, callback)`: Delete the paste with the specied ID. Requires username and password to be set.
  * `Edit(id, body, title, callback)`: Replace the specified paste. Requires username and password to be set.
  * `Raw(method, form, callback)`: Allows you to compose your own request, giving you access to [the full TinyPaste API](http://tny.cz/api/doc/). Parameters are passed in in the form of a `WWWForm` object.

### Room for Improvement
There's lots of opportunity to make this code more useful:
  * I didn't want to bog this code down with dependency on a JSON library... but if I did, [JSONObject](http://wiki.unity3d.com/index.php?title=JSONObject) is the one I'd pick. Parsing the server response here in this code would let me provide a higher-level API.
  * The choice of TinyPaste as the backing service was fairly arbitrary. Its two key features are optional authentication (so that I can just write to the service without having to report the paste ID back to the user), and an auth model that did not require cookies (which are not well supported in the WWW class). I'd be happy to entertain other services that shared these attributes.


