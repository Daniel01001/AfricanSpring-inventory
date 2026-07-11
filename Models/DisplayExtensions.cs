namespace AfricanSpringInventory.Models;

public static class DisplayExtensions
{
    public static string Badge(this StoreStatus s) => s switch
    {
        StoreStatus.Supplying => "supplying",
        StoreStatus.InTalks => "intalks",
        StoreStatus.Prospect => "prospect",
        StoreStatus.Declined => "declined",
        _ => ""
    };

    public static string Label(this StoreStatus s) => s switch
    {
        StoreStatus.InTalks => "In talks",
        _ => s.ToString()
    };

    public static string Label(this FridgeArrangement f) => f switch
    {
        FridgeArrangement.None => "No fridge",
        FridgeArrangement.StoreOwnsFridge => "Store owns fridge",
        FridgeArrangement.WeProvideFridge => "We provide fridge",
        _ => f.ToString()
    };

    public static string Label(this FridgeStatus s) => s switch
    {
        FridgeStatus.InStorage => "In storage",
        _ => s.ToString()
    };

    public static string Label(this PaymentMethod m) => m.ToString();
}
