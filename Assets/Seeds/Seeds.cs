﻿using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_IOS && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif
using UnityEngine;

public class Seeds : MonoBehaviour
{
    public static Seeds Instance { get; private set; }

    /// <summary>
    /// True if trace should be enabled, false otherwise.
    /// </summary>
    public bool TraceEnabled = false;

    /// <summary>
    /// True if Seeds SDK should auto-initialize during instantiation, false otherwise.
    /// </summary>
    public bool AutoInitialize = false;

    /// <summary>
    /// Server URL. Do not include trailing slash.
    /// </summary>
    public string ServerURL = "https://dash.playseeds.com";

    /// <summary>
    /// Application API key.
    /// </summary>
    public string ApplicationKey;

    public event Action<string> OnInAppMessageClicked;
	public event Action<string, decimal> OnInAppMessageClickedWithDynamicPrice;
	public event Action<string> OnInAppMessageDismissed;
    public event Action<string> OnInAppMessageLoadSucceeded;
    public event Action<string> OnInAppMessageShownSuccessfully;
    public event Action<string> OnInAppMessageShownInsuccessfully;
    public event Action<string> OnNoInAppMessageFound;
    public event Action OnAndroidIapServiceConnected;
    public event Action OnAndroidIapServiceDisconnected;
	public event Action<string, int, string> OnInAppPurchaseCount;
	public event Action<string, int, string> OnInAppMessageShowCount;
	public event Action<string, string, string> OnGenericUserBehaviorQuery;

#if UNITY_ANDROID && !UNITY_EDITOR
    private AndroidJavaObject androidInstance;
    private AndroidJavaObject androidBridgeInstance;
	private AndroidJavaObject androidInAppMessageShowCountListenerBridgeInstance;
	private AndroidJavaObject androidInAppPurchaseCountListenerBridgeInstance;
    private AndroidJavaObject androidUserBehaviorQueryListenerBridgeInstance;
    private AndroidJavaObject inAppBillingServiceConnection;
#endif

	#if UNITY_IOS && !UNITY_EDITOR
	[DllImport ("__Internal")]
	private static extern void Seeds_SetGameObjectName(string gameObjectName);

	[DllImport ("__Internal")]
	private static extern void Seeds_Setup(bool registerAsPlugin);
	#endif

    void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("Duplicate instance of Seeds. New instance will be destroyed.");
            enabled = false;
            DestroyObject(this);
            return;
        }

        DontDestroyOnLoad(this);
        Instance = this;

		#if UNITY_ANDROID && !UNITY_EDITOR
		using (var javaClass = new AndroidJavaClass("com.playseeds.android.sdk.Seeds"))
		{
		androidInstance = javaClass.CallStatic<AndroidJavaObject>("sharedInstance");
		}
		using (var javaClass = new AndroidJavaClass("com.playseeds.unity3d.androidbridge.InAppMessageListenerBridge"))
		{
		androidBridgeInstance = javaClass.CallStatic<AndroidJavaObject>("create", gameObject.name);
		}
		using (var javaClass = new AndroidJavaClass("com.playseeds.unity3d.androidbridge.InAppMessageShowCountListenerBridge"))
		{
		androidInAppMessageShowCountListenerBridgeInstance = javaClass.CallStatic<AndroidJavaObject>("create", gameObject.name);
		}
		using (var javaClass = new AndroidJavaClass("com.playseeds.unity3d.androidbridge.InAppPurchaseCountListenerBridge"))
		{
		androidInAppPurchaseCountListenerBridgeInstance = javaClass.CallStatic<AndroidJavaObject>("create", gameObject.name);
		}
    	using (var javaClass = new AndroidJavaClass("com.playseeds.unity3d.androidbridge.UserBehaviorQueryListenerBridge"))
		{
		androidUserBehaviorQueryListenerBridgeInstance  = javaClass.CallStatic<AndroidJavaObject>("create", gameObject.name);
		}
		using (var javaClass = new AndroidJavaClass("com.playseeds.unity3d.androidbridge.InAppBillingServiceConnection"))
		{
		inAppBillingServiceConnection = javaClass.CallStatic<AndroidJavaObject>("create", gameObject.name);
		}
		inAppBillingServiceConnection.Call("connect");
		#elif UNITY_IOS && !UNITY_EDITOR
		Seeds_SetGameObjectName(gameObject.name);
		Seeds_Setup(false);
		#endif

		if (AutoInitialize)
			Init(ServerURL, ApplicationKey);

		#if UNITY_ANDROID && !UNITY_EDITOR
		NotifyOnStart();
		#endif
    }

    void OnApplicationPause(bool pauseStatus)
    {
        if (TraceEnabled)
            Debug.Log(string.Format("[Seeds] OnApplicationPause({0})", pauseStatus));

        #if UNITY_ANDROID && !UNITY_EDITOR
        if (pauseStatus)
            NotifyOnStop();
        else
            NotifyOnStart();
        #endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private AndroidJavaObject CurrentActivity
    {
        get
        {
            using (var unityPlayerClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                return unityPlayerClass.GetStatic<AndroidJavaObject>("currentActivity");
            }
        }
    }

    private AndroidJavaObject AndroidIapService
    {
        get
        {
            if (!inAppBillingServiceConnection.Call<bool>("hasInAppBillingService"))
                return null;
            return inAppBillingServiceConnection.Call<AndroidJavaObject>("getInAppBillingService");
        }
    }

    private void RunOnUIThread(AndroidJavaRunnable runnable)
    {
        CurrentActivity.Call("runOnUiThread", new AndroidJavaRunnable(runnable));
    }

    private AndroidJavaObject CreateMapFromDicrionary(IDictionary<string, string> dictionary)
    {
        AndroidJavaObject map;
        using (var helpersClass = new AndroidJavaClass("com.playseeds.unity3d.androidbridge.Helpers"))
        {
            map = helpersClass.CallStatic<AndroidJavaObject>("createHashMapOfStringString");
        }

        foreach (var entry in dictionary)
        {
            map.Call<AndroidJavaObject>("put", entry.Key, entry.Value);
        }

        return map;
    }

    private AndroidJavaObject CreateListFromEnumerable(IEnumerable<string> enumerable)
    {
        AndroidJavaObject list;
        using (var helpersClass = new AndroidJavaClass("com.playseeds.unity3d.androidbridge.Helpers"))
        {
            list = helpersClass.CallStatic<AndroidJavaObject>("createArrayListOfString");
        }

        foreach (var item in enumerable)
        {
            list.Call<bool>("add", item);
        }

        return list;
    }
#endif

    private static void NotImplemented(string method)
    {
        Debug.LogWarning(string.Format("Method {0} not implemented for current platform. Please build for iOS or Android to test.", method));
    }

    void inAppMessageClicked(string messageId)
    {
        if (TraceEnabled)
            Debug.Log(string.Format("[Seeds] OnInAppMessageClicked({0})", messageId));

        if (OnInAppMessageClicked != null)
            OnInAppMessageClicked(messageId);
    }

	void inAppMessageClickedWithDynamicPrice(string msg)
	{
		var msgParts = msg.Split(' ');
		var messageId = msgParts[0];
		var price = Convert.ToDecimal(msgParts[1]);

		if (TraceEnabled)
			Debug.Log(string.Format("[Seeds] OnInAppMessageClickedWithDynamicPrice({0}, {1})", messageId, price));

		if (OnInAppMessageClickedWithDynamicPrice != null)
			OnInAppMessageClickedWithDynamicPrice(messageId, price);
	}

    void inAppMessageDismissed(string messageId)
    {
        if (TraceEnabled)
            Debug.Log(string.Format ("[Seeds] OnInAppMessageDismissed({0})", messageId));

        if (OnInAppMessageDismissed != null)
            OnInAppMessageDismissed(messageId);
    }

    void inAppMessageLoadSucceeded(string messageId)
    {
        if (TraceEnabled)
            Debug.Log(string.Format("[Seeds] OnInAppMessageLoadSucceeded({0})", messageId));

        if (OnInAppMessageLoadSucceeded != null)
            OnInAppMessageLoadSucceeded(messageId);
    }

    void inAppMessageShownSuccessfully(string messageId)
    {
        if (TraceEnabled)
            Debug.Log(string.Format("[Seeds] OnInAppMessageShownSuccessfully({0})", messageId));

        if (OnInAppMessageShownSuccessfully != null)
            OnInAppMessageShownSuccessfully(messageId);
    }

    void inAppMessageShownInsuccessfully(string messageId)
    {
        if (TraceEnabled)
            Debug.Log(string.Format("[Seeds] OnInAppMessageShownInsuccessfully({0})", messageId));

        if (OnInAppMessageShownInsuccessfully != null)
            OnInAppMessageShownInsuccessfully(messageId);
    }

    void noInAppMessageFound(string messageId)
    {
        if (TraceEnabled)
            Debug.Log(string.Format("[Seeds] OnNoInAppMessageFound({0})", messageId));

        if (OnNoInAppMessageFound != null)
            OnNoInAppMessageFound(messageId);
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    void onAndroidIapServiceConnected(string notUsed)
    {
        if (TraceEnabled)
            Debug.Log("[Seeds] OnAndroidIapServiceConnected()");

        if (AutoInitialize)
            Init(ServerURL, ApplicationKey);

        if (OnAndroidIapServiceConnected != null)
            OnAndroidIapServiceConnected();
    }

    void onAndroidIapServiceDisconnected(string notUsed)
    {
        if (TraceEnabled)
            Debug.Log("[Seeds] OnAndroidIapServiceDisconnected()");

        if (AutoInitialize)
            Init(ServerURL, ApplicationKey);

        if (OnAndroidIapServiceDisconnected != null)
            OnAndroidIapServiceDisconnected();
    }
#endif

    [Serializable]
    public class InAppMessageShowCount
    {
		public string errorMessage;
        public string messageId;
        public int showCount;
    }

    void onInAppMessageShowCount (String json)
    {
        if (TraceEnabled)
			Debug.Log (string.Format ("[Seeds] OnInAppMessageShowCount({0})", json));

		InAppMessageShowCount stats = JsonUtility.FromJson<InAppMessageShowCount> (json);
        if (OnInAppMessageShowCount != null)
			OnInAppMessageShowCount (stats.errorMessage, stats.showCount, stats.messageId);
    }

    [Serializable]
    public class InAppPurchaseCount
    {
		public string errorMessage;
        public string key;
        public int purchasesCount;
    }

	void onInAppPurchaseCount (String json)
    {
        if (TraceEnabled)
			Debug.Log (string.Format ("[Seeds] OnInAppPurchaseCount({0})", json));

		InAppPurchaseCount stats = JsonUtility.FromJson<InAppPurchaseCount> (json);
		if (OnInAppPurchaseCount != null)
			OnInAppPurchaseCount (stats.errorMessage, stats.purchasesCount, stats.key);
    }

	[Serializable]
	public class GenericUserBehaviorQuery
	{
		public string errorMessage;
		public string queryPath;
		public string result;
	}

	void onGenericUserBehaviorQuery (String json)
	{
		if (TraceEnabled)
			Debug.Log (string.Format ("[Seeds] OnGenericUserBehaviorQuery({0})", json));

		GenericUserBehaviorQuery stats = JsonUtility.FromJson<GenericUserBehaviorQuery> (json);
		if (OnGenericUserBehaviorQuery != null)
			OnGenericUserBehaviorQuery (stats.errorMessage, stats.result, stats.queryPath);
	}

#if UNITY_IOS && !UNITY_EDITOR
    [DllImport ("__Internal")]
    private static extern void Seeds_Init(string serverUrl, string appKey);
#endif

    public Seeds Init(string serverUrl, string appKey)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        var currentActivity = CurrentActivity;
        if (currentActivity == null)
            Debug.LogWarning("[Seeds] CurrentActivity is null");
        var androidIapService = AndroidIapService;
        if (androidIapService == null)
            Debug.LogWarning("[Seeds] AndroidIapService is null");
        RunOnUIThread(() => androidInstance.Call<AndroidJavaObject>("init", currentActivity, androidIapService, androidBridgeInstance, serverUrl,
            appKey));
#elif UNITY_IOS && !UNITY_EDITOR
        Seeds_Init(serverUrl, appKey);
#else
        NotImplemented("Init(string serverUrl, string appKey)");
#endif

        return this;
    }

#if UNITY_IOS && !UNITY_EDITOR
    [DllImport ("__Internal")]
    private static extern void Seeds_InitWithDeviceId(string serverUrl, string appKey, string deviceId);
#endif

    public Seeds Init(string serverUrl, string appKey, string deviceId)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        var currentActivity = CurrentActivity;
        if (currentActivity == null)
            Debug.LogWarning("[Seeds] CurrentActivity is null");
        var androidIapService = AndroidIapService;
        if (androidIapService == null)
            Debug.LogWarning("[Seeds] AndroidIapService is null");
        RunOnUIThread(() => androidInstance.Call<AndroidJavaObject>("init", currentActivity, androidIapService, androidBridgeInstance, serverUrl,
            appKey, deviceId));
#elif UNITY_IOS && !UNITY_EDITOR
        Seeds_InitWithDeviceId(serverUrl, appKey, deviceId);
#else
        NotImplemented("Init(string serverUrl, string appKey, string deviceId)");
#endif

        return this;
    }

#if UNITY_IOS && !UNITY_EDITOR
    [DllImport ("__Internal")]
    private static extern bool Seeds_IsStarted();
#endif

    public bool IsInitialized
    {
        get
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return androidInstance.Call<bool>("isInitialized");
#elif UNITY_IOS && !UNITY_EDITOR
            return Seeds_IsStarted();
#else
            NotImplemented("IsInitialized::get");
            return false;
#endif
        }
    }

    public void Halt()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        androidInstance.Call("halt");
#else
        NotImplemented("Halt()");
#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    public void NotifyOnStart()
    {
        RunOnUIThread(() => {
            if (androidInstance != null)
                androidInstance.Call("onStart");
        });
    }

    public void NotifyOnStop()
    {
        RunOnUIThread(() => {
            if (androidInstance != null)
                androidInstance.Call("onStop");
        });
    }

    public void NotifyOnGCMRegistrationId(string registrationId)
    {
        androidInstance.Call("onRegistrationId", registrationId);
    }
#endif

#if UNITY_IOS && !UNITY_EDITOR
    [DllImport ("__Internal")]
    private static extern void Seeds_RecordEvent1(string key);
#endif

    public void RecordEvent(string key)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        androidInstance.Call("recordEvent", key);
#elif UNITY_IOS && !UNITY_EDITOR
        Seeds_RecordEvent1(key);
#else
        NotImplemented("RecordEvent(string key)");
#endif
    }

#if UNITY_IOS && !UNITY_EDITOR
    [DllImport ("__Internal")]
    private static extern void Seeds_RecordEvent2(string key, int count);
#endif

    public void RecordEvent(string key, int count)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        androidInstance.Call("recordEvent", key, count);
#elif UNITY_IOS && !UNITY_EDITOR
        Seeds_RecordEvent2(key, count);
#else
        NotImplemented("RecordEvent(string key, int count)");
#endif
    }

#if UNITY_IOS && !UNITY_EDITOR
    [DllImport ("__Internal")]
    private static extern void Seeds_RecordEvent3(string key, int count, double sum);
#endif

    public void RecordEvent(string key, int count, double sum)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        androidInstance.Call("recordEvent", key, count, sum);
#elif UNITY_IOS && !UNITY_EDITOR
        Seeds_RecordEvent3(key, count, sum);
#else
        NotImplemented("RecordEvent(string key, int count, double sum)");
#endif
    }

    public void RecordEvent(string key, IDictionary<string, string> segmentation, int count)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        androidInstance.Call("recordEvent", key, CreateMapFromDicrionary(segmentation), count);
#else
        NotImplemented("RecordEvent(string key, IDictionary<string, string> segmentation, int count)");
#endif
    }

    public void RecordEvent(string key, IDictionary<string, string> segmentation, int count, double sum)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        androidInstance.Call("recordEvent", key, CreateMapFromDicrionary(segmentation), count, sum);
#else
        NotImplemented("RecordEvent(string key, IDictionary<string, string> segmentation, int count, double sum)");
#endif
    }

#if UNITY_IOS && !UNITY_EDITOR
    [DllImport ("__Internal")]
    private static extern void Seeds_RecordIAPEvent(string key, double price, string transactionId);
#endif

    public void RecordIAPEvent(string key, double price)
    {
        RecordIAPEvent(key, price, null);
    }

    public void RecordIAPEvent(string key, double price, string transactionId)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        androidInstance.Call("recordIAPEvent", key, price, transactionId);
#elif UNITY_IOS && !UNITY_EDITOR
        Seeds_RecordIAPEvent(key, price, transactionId);
#else
        NotImplemented("RecordIAPEvent(string key, double price, string transactionId)");
#endif
    }

#if UNITY_IOS && !UNITY_EDITOR
    [DllImport ("__Internal")]
    private static extern void Seeds_RecordSeedsIAPEvent(string key, double price, string transactionId);
#endif

    public void RecordSeedsIAPEvent(string key, double price)
    {
        RecordSeedsIAPEvent(key, price, null);
    }

    public void RecordSeedsIAPEvent(string key, double price, string transactionId)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        androidInstance.Call("recordSeedsIAPEvent", key, price, transactionId);
#elif UNITY_IOS && !UNITY_EDITOR
        Seeds_RecordSeedsIAPEvent(key, price, transactionId);
#else
        NotImplemented("RecordSeedsIAPEvent(string key, double price, string transactionId)");
#endif
    }

    public Seeds SetUserData(IDictionary<string, string> data)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        androidInstance.Call<AndroidJavaObject>("setUserData", CreateMapFromDicrionary(data));
#else
        NotImplemented("SetUserData(IDictionary<string, string> data)");
#endif

        return this;
    }

    public Seeds SetUserData(IDictionary<string, string> data, IDictionary<string, string> customData)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        androidInstance.Call<AndroidJavaObject>("setUserData", CreateMapFromDicrionary(data),
            CreateMapFromDicrionary(customData));
#else
        NotImplemented("SetUserData(IDictionary<string, string> data, IDictionary<string, string> customData)");
#endif

        return this;
    }

    public Seeds SetCustomUserData(IDictionary<string, string> customData)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        androidInstance.Call<AndroidJavaObject>("setCustomUserData", CreateMapFromDicrionary(customData));
#else
        NotImplemented("SetCustomUserData(IDictionary<string, string> customData)");
#endif

        return this;
    }

#if UNITY_IOS && !UNITY_EDITOR
    [DllImport ("__Internal")]
    private static extern void Seeds_SetLocation(double lat, double lon);
#endif

    public Seeds SetLocation(double lat, double lon)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        androidInstance.Call<AndroidJavaObject>("setLocation", lat, lon);
#elif UNITY_IOS && !UNITY_EDITOR
        Seeds_SetLocation(lat, lon);
#else
        NotImplemented("SetLocation(double lat, double lon)");
#endif

        return this;
    }

    public Seeds SetCustomCrashSegments(IDictionary<string, string> segments)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        androidInstance.Call<AndroidJavaObject>("setCustomCrashSegments", CreateMapFromDicrionary(segments));
#else
        NotImplemented("SetCustomCrashSegments(IDictionary<string, string> segments)");
#endif

        return this;
    }

    public Seeds AddCrashLog(string record)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        androidInstance.Call<AndroidJavaObject>("addCrashLog", record);
#else
        NotImplemented("AddCrashLog(string record)");
#endif

        return this;
    }

    public Seeds LogException(string exception)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        androidInstance.Call<AndroidJavaObject>("logException", exception);
#else
        NotImplemented("LogException(string exception)");
#endif

        return this;
    }

#if UNITY_IOS && !UNITY_EDITOR
    [DllImport ("__Internal")]
    private static extern void Seeds_EnableCrashReporting();
#endif

    public Seeds EnableCrashReporting()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        androidInstance.Call<AndroidJavaObject>("enableCrashReporting");
#elif UNITY_IOS && !UNITY_EDITOR
        Seeds_EnableCrashReporting();
#else
        NotImplemented("EnableCrashReporting()");
#endif

        return this;
    }

    public Seeds SetDisableUpdateSessionRequests(bool disable)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        androidInstance.Call<AndroidJavaObject>("setDisableUpdateSessionRequests", disable);
#else
        NotImplemented("SetDisableUpdateSessionRequests(bool disable)");
#endif

        return this;
    }

    public Seeds SetLoggingEnabled(bool enabled)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        androidInstance.Call<AndroidJavaObject>("setLoggingEnabled", enabled);
#else
        NotImplemented("SetLoggingEnabled(bool enabled)");
#endif

        return this;
    }

    public bool IsLoggingEnabled
    {
        get
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return androidInstance.Call<bool>("isLoggingEnabled");
#else
            NotImplemented("IsLoggingEnabled::get");
            return false;
#endif
        }
        set
        {
            SetLoggingEnabled(value);
        }
    }

    public Seeds EnablePublicKeyPinning(IEnumerable<string> certificates)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        androidInstance.Call<AndroidJavaObject>("enablePublicKeyPinning", CreateListFromEnumerable(certificates));
#else
        NotImplemented("EnablePublicKeyPinning(IEnumerable<string> certificates)");
#endif

        return this;
    }

#if UNITY_IOS && !UNITY_EDITOR
    [DllImport ("__Internal")]
    private static extern void Seeds_RequestInAppMessage(string messageId, string manualLocalizedPrice);
#endif

    public void RequestInAppMessage(string messageId, string manualLocalizedPrice)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        androidInstance.Call("requestInAppMessage", messageId, manualLocalizedPrice);
#elif UNITY_IOS && !UNITY_EDITOR
        Seeds_RequestInAppMessage(messageId, manualLocalizedPrice);
#else
        NotImplemented("RequestInAppMessage(string messageId, string manualLocalizedPrice)");
#endif
    }

    public void RequestInAppMessage(string messageId)
    {
        RequestInAppMessage(messageId, null);
    }

    public void RequestInAppMessage()
    {
        RequestInAppMessage(null, null);
    }

#if UNITY_IOS && !UNITY_EDITOR
    [DllImport ("__Internal")]
    private static extern bool Seeds_IsInAppMessageLoaded(string messageId);
#endif

    public bool IsInAppMessageLoaded(string messageId)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        return androidInstance.Call<bool>("isInAppMessageLoaded", messageId);
#elif UNITY_IOS && !UNITY_EDITOR
        return Seeds_IsInAppMessageLoaded(messageId);
#else
        NotImplemented("IsInAppMessageLoaded(string messageId)");
        return false;
#endif
    }

    public bool IsInAppMessageLoaded()
    {
        return IsInAppMessageLoaded(null);
    }

#if UNITY_IOS && !UNITY_EDITOR
    [DllImport ("__Internal")]
    private static extern void Seeds_ShowInAppMessage(string messageId, string context);
#endif

    public void ShowInAppMessage(string messageId, string context)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        androidInstance.Call("showInAppMessage", messageId, context);
#elif UNITY_IOS && !UNITY_EDITOR
        Seeds_ShowInAppMessage(messageId, context);
#else
        NotImplemented("ShowInAppMessage(string messageId, string context)");
#endif
    }

    public void ShowInAppMessage (string messageId)
    {
        ShowInAppMessage (messageId, null);
    }

    public void ShowInAppMessage()
    {
        ShowInAppMessage(null, null);
    }

    public void RequestTotalInAppPurchaseCount ()
    {
        RequestInAppPurchaseCount (null);
    }

    public void RequestInAppPurchaseCount (string key)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        androidInstance.Call("requestInAppPurchaseCount", key, androidInAppPurchaseCountListenerBridgeInstance);
#elif UNITY_IOS && !UNITY_EDITOR
        Seeds_RequestInAppPurchaseCount(key);
#else
        NotImplemented ("RequestInAppPurchaseCount(string key)");
#endif
    }

#if UNITY_IOS && !UNITY_EDITOR
    [DllImport ("__Internal")]
    private static extern void Seeds_RequestInAppPurchaseCount(string key);
#endif

    public void RequestTotalInAppMessageShowCount ()
    {
		RequestInAppMessageShowCount (null);
    }

	public void RequestInAppMessageShowCount (string messageId)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
		androidInstance.Call("requestInAppMessageShowCount", messageId, androidInAppMessageShowCountListenerBridgeInstance);
#elif UNITY_IOS && !UNITY_EDITOR
		Seeds_RequestInAppMessageShowCount(messageId);
#else
        NotImplemented ("RequestInAppMessageStats(string key)");
#endif
    }

#if UNITY_IOS && !UNITY_EDITOR
    [DllImport ("__Internal")]
	private static extern void Seeds_RequestInAppMessageShowCount(string key);
#endif

	public void RequestGenericUserBehaviorQuery (string queryPath)
	{
#if UNITY_ANDROID && !UNITY_EDITOR
		androidInstance.Call("requestGenericUserBehaviorQuery", queryPath, androidUserBehaviorQueryListenerBridgeInstance);
#elif UNITY_IOS && !UNITY_EDITOR
		Seeds_RequestGenericUserBehaviorQuery(queryPath);
#else
		NotImplemented ("RequestGenericUserBehaviorQuery(string key)");
#endif
	}

#if UNITY_IOS && !UNITY_EDITOR
	[DllImport ("__Internal")]
	private static extern void Seeds_RequestGenericUserBehaviorQuery(string queryPath);
#endif
}
