using UnityEngine;
using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(InAppStore))]
public class StoreHandler : MonoBehaviour
{
    public string publicKey;
    public string payload;
    public MarketType market;

    private AndroidJavaObject pluginUtilsClass = null;
    private AndroidJavaObject pluginUtilsClass1 = null;
    private AndroidJavaObject activityContext = null;
    private AndroidJavaObject activityContext1 = null;
    private IBillingListener listener;

    public void SetUpBillingService(IBillingListener listener)
    {
        if (pluginUtilsClass == null)
        {
            this.listener = listener;
            using (AndroidJavaClass activityClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                activityContext = activityClass.GetStatic<AndroidJavaObject>("currentActivity");
            }

            if (market == MarketType.Myket)
            {
                pluginUtilsClass = new AndroidJavaObject("ir.myket.unity.iab.MyketBillingService", activityContext,
                    publicKey, "ir.mservices.market", "ir.mservices.market.InAppBillingService.BIND");
            }
            else if (market == MarketType.GooglePlay)
            {
                pluginUtilsClass = new AndroidJavaObject("ir.myket.unity.iab.MyketBillingService", activityContext,
                    publicKey, "com.vending.android", "com.vending.android.InAppBillingService.BIND");
            }
            else if (market == MarketType.Bazaar)
            {
                pluginUtilsClass = new AndroidJavaObject("ir.myket.unity.iab.MyketBillingService", activityContext,
                    publicKey, "com.farsitel.bazaar", "ir.cafebazaar.pardakht.InAppBillingService.BIND");
            }

            if (pluginUtilsClass != null)
            {
                pluginUtilsClass.Call("startBillingService");
            }
        }
    }

    public void OnBillingServiceSetup()
    {
        listener.OnBillingServiceSetupFinished();
    }

    public void UpdatePurchases()
    {
        if (pluginUtilsClass != null)
        {
            pluginUtilsClass.Call("updatePurchases");
        }
    }

    public void OnPurchasesUpdateFinished(string inventoryString)
    {
        JSONNode purchaseResult = JSON.Parse(inventoryString);
        string purchasesData = purchaseResult["purchases"].ToString();
        JSONArray jSONArrayPurchases = (JSONArray) JSONArray.Parse(purchasesData);
        List<Purchase> purchaseList = new List<Purchase>();
        for (int i = 0; i < jSONArrayPurchases.Count; i++)
        {
            purchaseList.Add(GetPurchaseData(jSONArrayPurchases[i]));
        }

        GetComponent<InAppStore>().OnPurchasesUpdated(purchaseList);
    }

    public void UpdatePurchasesAndDetails(Product[] products)
    {
        AndroidJavaObject arraylistItemSkus = new AndroidJavaObject("java.util.ArrayList");
        AndroidJavaObject arraylistSubsSkus = new AndroidJavaObject("java.util.ArrayList");

        for (int i = 0; i < products.Length; i++)
        {
            if (products[i].type == Product.ProductType.Subscription)
            {
                arraylistSubsSkus.Call<bool>("add", new AndroidJavaObject("java.lang.String", products[i].productId));
            }
            else
            {
                arraylistItemSkus.Call<bool>("add", new AndroidJavaObject("java.lang.String", products[i].productId));
            }
        }

        if (pluginUtilsClass != null)
        {
            pluginUtilsClass.Call("updatePurchasesAndDetails", arraylistItemSkus, arraylistSubsSkus);
        }
    }

    public void OnPurchasesAndDetailsUpdateFinished(string inventoryString)
    {
        //todo change it here
        Debug.Log("finished");
        JSONNode purchaseResult = JSON.Parse(inventoryString);
        string purchasesData = purchaseResult["purchases"].ToString();
        string skuDetails = purchaseResult["details"].ToString();
        JSONArray jSONArrayPurchases = (JSONArray) JSONArray.Parse(purchasesData);
        JSONArray jSONArrayDetails = (JSONArray) JSONArray.Parse(skuDetails);
        List<Purchase> purchaseList = new List<Purchase>();
        List<ProductDetail> productDetailList = new List<ProductDetail>();

        for (int i = 0; i < jSONArrayPurchases.Count; i++)
        {
            purchaseList.Add(GetPurchaseData(jSONArrayPurchases[i]));
        }

        for (int j = 0; j < jSONArrayDetails.Count; j++)
        {
            productDetailList.Add(GetProductDetailData(jSONArrayDetails[j]));
        }

        listener.OnPurchasesAndDetailsUpdated(purchaseList, productDetailList);
    }

    public void BuyProduct(string produc_sku, string type)
    {
        if (pluginUtilsClass != null)
        {
            pluginUtilsClass.Call("launchPurchaseFlow", produc_sku, type, payload);
        }
    }


    public void OnUserCancel(string message)
    {
        //todo here
        GetComponent<InAppStore>().checkIfUserHasProduct(0);

//		listener.OnUserCancelPurchase (message);
    }

    public void OnPurchaseProductFinished(string purchaseJson)
    {
        listener.OnPurchaseFinished(GetPurchaseData(purchaseJson));
    }

    public void ConsumePurchase(string productId, string purchaseToken)
    {
        if (pluginUtilsClass != null)
        {
            pluginUtilsClass.Call("consume", productId, purchaseToken);
        }
    }

    public void OnConsumeProductFinished(string purchaseJson)
    {
        JSONNode purchaseInfo = JSON.Parse(purchaseJson);
        string productId = purchaseInfo["productId"].Value.ToString();
        string purchaseToken = purchaseInfo["purchaseToken"].Value.ToString();
        listener.OnConsumeFinished(productId, purchaseToken);
    }


    public void OnErrorOccurred(string errorJson)
    {
        JSONNode purchaseResult = JSON.Parse(errorJson);
        int errorCode = purchaseResult["code"].AsInt;
        string errorMessage = purchaseResult["message"].Value.ToString();
        listener.OnError(errorCode, errorMessage);
    }

    public void UserIsLoggedIn(String isLogged)
    {
        Debug.Log("logged: " + isLogged);
    }

    public void checkUserLogged()
    {
        using (AndroidJavaClass activityClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        {
            activityContext1 = activityClass.GetStatic<AndroidJavaObject>("currentActivity");
        }

        pluginUtilsClass1 = new AndroidJavaObject("com.narmafzarsazan.bazaar.StartLogin", activityContext, false);
    }

    private Purchase GetPurchaseData(string data)
    {
        JSONNode info = JSONNode.Parse(data);
        Purchase purchase = new Purchase();
        purchase.orderId = info["orderId"].Value.ToString();
        purchase.purchaseToken = info["purchaseToken"].Value.ToString();
        purchase.payload = info["developerPayload"].Value.ToString();
        purchase.packageName = info["packageName"].Value.ToString();
        purchase.purchaseState = info["purchaseState"].AsInt;
        purchase.purchaseTime = info["purchaseTime"].Value.ToString();
        purchase.productId = info["productId"].Value.ToString();
        purchase.json = data;
        return purchase;
    }


    private ProductDetail GetProductDetailData(string data)
    {
        JSONNode info = JSONNode.Parse(data);
        ProductDetail productDetail = new ProductDetail();
        productDetail.productId = info["productId"].Value.ToString();
        productDetail.title = info["title"].Value.ToString();
        productDetail.description = info["description"].Value.ToString();
        productDetail.price = info["price"].Value.ToString();
        productDetail.type = info["type"].Value.ToString();

        return productDetail;
    }

    public void DebugLog(string msg)
    {
        Debug.Log(msg);
    }


    void OnApplicationQuit()
    {
        if (pluginUtilsClass != null)
        {
            pluginUtilsClass.Call("stopBillingService");
        }
    }
}

public class Purchase
{
    public string orderId;
    public string purchaseToken;
    public string payload;
    public string packageName;
    public int purchaseState;
    public string purchaseTime;
    public string productId;
    public string json;
}


public class ProductDetail
{
    public string productId;
    public string title;
    public string description;
    public string price;
    public string type;
}

[Serializable]
public class Product
{
    public enum ProductType
    {
        InApp,
        Subscription
    };

    public string productId;
    public ProductType type;
}

public enum MarketType
{
    Myket,
    GooglePlay,
    Bazaar
};

public interface IBillingListener
{
    void OnBillingServiceSetupFinished();

    void OnPurchasesUpdated(List<Purchase> purchases);

    void OnPurchasesAndDetailsUpdated(List<Purchase> purchases, List<ProductDetail> products);

    void OnUserCancelPurchase(string errorMessage);

    void OnPurchaseFinished(Purchase purchase);

    void OnConsumeFinished(string productId, string purchaseToken);

    void OnError(int errorCode, string errorMessage);
}