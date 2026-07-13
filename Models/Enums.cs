namespace AfricanSpringInventory.Models;

public enum UserRole
{
    Owner,
    Friend
}

public enum StoreStatus
{
    Prospect,
    InTalks,
    Supplying,
    Declined
}

public enum FridgeArrangement
{
    None,
    StoreOwnsFridge,
    WeProvideFridge
}

public enum FridgeStatus
{
    InStorage,
    Installed,
    Removed,
    Faulty
}

public enum PaymentMethod
{
    Cash,
    EFT,
    Other
}

public enum StockMovementType
{
    Production,
    Delivery,
    Adjustment
}

public enum OrderStatus
{
    New,
    Confirmed,
    OutForDelivery,
    Delivered,
    Paid,
    Cancelled
}
