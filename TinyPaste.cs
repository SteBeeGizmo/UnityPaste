using UnityEngine;
using System.Collections;

public class TinyPaste : MonoBehaviour
{
	protected const string _kTinyPasteAPI = "http://tny.cz/api";

	public enum ResponseFormatType
	{
		JSON,
		XML
	}
	public ResponseFormatType ResponseFormat = ResponseFormatType.JSON;

	public string Username;
	public string Password;

	public delegate void TinyPasteResponse(string response, string error);

	protected string _cachedHash;
	protected string _cachedFor = null;
	protected string composeCredentials()
	{
		if (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
			return "";

		if (string.IsNullOrEmpty(_cachedHash) || Password != _cachedFor)
		{
			System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create();
			byte[] bits = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(Password));
			System.Text.StringBuilder hex = new System.Text.StringBuilder(bits.Length * 2);
			for (int i = 0; i < bits.Length; i++)
				hex.AppendFormat("{0:x2}", bits[i]);
			_cachedHash = hex.ToString();
			_cachedFor = Password;
		}

		return string.Format("{0}:{1}", Username, _cachedHash);
	}

	public void Create(string body, string title, params object[] callback)
	{
		WWWForm form = new WWWForm();

		form.AddField("paste", body);

		if (!string.IsNullOrEmpty(title))
			form.AddField("title", title);

		string auth = composeCredentials();
		if (!string.IsNullOrEmpty(auth))
			form.AddField("authenticate", auth);

		Raw(MethodType.Create, form, callback);
	}

	public void Get(string id, params object[] callback)
	{
		WWWForm form = new WWWForm();

		form.AddField("id", id);

		Raw(MethodType.Get, form, callback);
	}

	public enum SortType
	{
		Default,
		Newest,
		Oldest
	}
	public void List(params object[] callback)
	{
		WWWForm form = new WWWForm();

		form.AddField("authenticate", composeCredentials());

		Raw(MethodType.List, form, callback);
	}

	public void Delete(string id, params object[] callback)
	{
		WWWForm form = new WWWForm();
		
		form.AddField("id", id);
		form.AddField("authenticate", composeCredentials());

		Raw(MethodType.Delete, form, callback);
	}

	public void Edit(string id, string body, string title, params object[] callback)
	{
		WWWForm form = new WWWForm();
		
		form.AddField("id", id);
		form.AddField("authenticate", composeCredentials());

		if (!string.IsNullOrEmpty(body))
			form.AddField("paste", body);
		
		if (!string.IsNullOrEmpty(title))
			form.AddField("title", title);

		Raw(MethodType.Edit, form, callback);
	}

	public enum MethodType
	{
		Create,
		Get,
		List,
		Delete,
		Edit
	}

	/*
	 * See http://tny.cz/api/doc/ for a list of available methods, and the
	 * 	optional parameters you might find useful for each one
	 * */
	public void Raw(MethodType method, WWWForm form, params object[] callback)
	{
		string url = string.Format("{0}/{1}.{2}", _kTinyPasteAPI, method.ToString().ToLower(), ResponseFormat.ToString());

		bool callbackValid = false;
		if (callback != null && callback.Length > 0)
		{
			if (callback[0] is GameObject)
			{
				string function = "OnTinyPasteSucceeded";
				if (callback.Length > 1 && callback[1] is string)
					function = callback[1] as string;
				
				string errFunc = "OnTinyPasteFailed";
				if (callback.Length > 2 && callback[2] is string)
					errFunc = callback[2] as string;
				
				StartCoroutine(apiCall(url, form, (response, error) => {
					if (!string.IsNullOrEmpty(error))
						((GameObject)callback[0]).SendMessage(errFunc, error, SendMessageOptions.DontRequireReceiver);
					else
						((GameObject)callback[0]).SendMessage(function, response, SendMessageOptions.DontRequireReceiver);
				}));
				callbackValid = true;
			}
			else if (callback[0] is TinyPasteResponse)
			{
				StartCoroutine(apiCall(url, form, null));
				callbackValid = true;
			}
		}
		
		if (!callbackValid)
			StartCoroutine(apiCall(url, form, (response, error) => {
				if (!string.IsNullOrEmpty(error))
					Debug.LogWarning(error);
			}));
	}
	
	protected IEnumerator apiCall(string url, WWWForm form, TinyPasteResponse callback)
	{
		WWW request = new WWW(url, form);
		yield return request;

		if (callback != null)
		{
			if (!string.IsNullOrEmpty(request.error))
				callback(null, request.error);
			else
				callback(request.text, null);
		}
	}
}
