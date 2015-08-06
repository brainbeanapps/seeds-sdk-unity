#Seeds Unity Package for Android

#Seeds
[Seeds](http://www.playseeds.com) increases paying user conversion for freemium mobile games motivating users to make their first purchase by letting them know that their purchase will help finance microloans in the developing world. The SDK implements this with an interstitial ad and event tracking analytics.

We also have available:
[API](https://github.com/therealseeds/seeds-public-api)
[Android SDK](https://github.com/therealseeds/seeds-sdk-android)

##The Seeds Unity SDK

The Seeds Unity Package is a wrapper for our Android SDK which is itself built with production-tested open source components including [Countly Android SDK](https://github.com/Countly/seeds-sdk-android).

##Overview
All you need to get started with integration is the special app_key “test”. This will show a test interstitial ad after you've installed the SDK.

After testing, please make a deep link (see [how to make a deep link](https://developer.android.com/training/app-indexing/deep-linking.html)) to the in-app purchase item you’d like to promote (in-game virtual currency is a great choice).

If you haven't already, please also let us know your game name, in-app item and the deep-link URL to [sungwon@playseeds.com](sungwon@playseeds.com).

We will then set you up with an app_key specific to your game and make a custom ad for your game item.

##Installation

###1. Add the Package to your project

####Download the package

Download the package:
- [Unity 4.x](https://bintray.com/artifact/download/seedsinc/maven/unity-sdk%20/%200.2.2/SeedsSDK022_Unity4.unitypackage)
- [Unity 5.x](https://bintray.com/artifact/download/seedsinc/maven/unity-sdk%20/%200.2.2/SeedsSDK022_Unity5.unitypackage)

####Import the package

From the Unity editor "Assets" menu, select "Import Package" => "Custom Package..." and select the package you downloaded.

(Note: if you get an error saying assets/plugins or other folders are mapped to something else. Try the following workaround: 1) rename the folders with errors 2) import the Seeds package 3) rename the original folders back to their original names.)

###2. Configure the Seeds prefab

Select the Seeds prefab in the Project view in Assets/Seeds and in the inspector panel input "http://dash.playseeds.com" as your Server URL and use "test" (or your own app_key if you have one already) for the Application Key. Make sure that the "Auto Initialize" check box is checked.

###3. Display the Seeds promo interstitial

Use the Seeds instance to load:

```c#
Seeds.Instance.RequestInAppMessage();
```

and show:

```c#
Seeds.Instance.ShowInAppMessage();
```

the interstitial promo in your code.

You may wish to add a helper method to your activity to accomplish these functions, e.g.:

```c#
public void ShowOrLoadSeedsPromo()
    {
        if (Seeds.Instance.IsInAppMessageLoaded)
            Seeds.Instance.ShowInAppMessage();
        else
            Seeds.Instance.RequestInAppMessage();
    }
```

###4. Track the item purchase

In your item store code, please include the following tracking code after a purchase of the Seeds-promoted item:

```c#
Seeds.Instance.RecordSeedsIAPEvent("ITEM", PRICE);
```

and for regular non-Seeds-promoted items:

```c#
 Seeds.Instance.RecordIAPEvent("ITEM", PRICE);
```

where ITEM is a string that is the name or SKU of the item and PRICE is a double representing the price of the item.

## Troubleshooting

###AndroidManifest errors upon Building

If you are getting an error like `Main manifest has <uses-sdk android:minSdkVersion='10'> but library uses minSdkVersion='12'`, this is because Seeds requires a minimum Android API level of 12. To change your project's Android build settings, from the menu select “File” => “Build Settings”, then select “Android”, then “Player Settings” and then in the Inspector scroll down to “Minimum API Level” in the “Identification” section and change the value to “API Level 12"

###Google Play Services-related errors upon Building

Our SDK comes with google play services. If you already have google play services installed, you may have to remove the duplicate files

## Support

Open an issue or email [sungwon@playseeds.com](sungwon@playseeds.com)
