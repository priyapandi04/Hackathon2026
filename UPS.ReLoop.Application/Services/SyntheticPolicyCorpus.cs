namespace UPS.ReLoop.Application.Services;

using UPS.ReLoop.Application.Interfaces;

/// <summary>
/// SYNTHETIC retailer return/resale policy corpus — fabricated for the demo.
/// UPS and retailers do not share real policy data, so this stands in as the
/// knowledge base that the policy RAG retriever searches over. Each entry keeps a
/// stable policy reference so decisions stay auditable and reproducible.
/// </summary>
public static class SyntheticPolicyCorpus
{
    public static IReadOnlyList<PolicyDocument> Documents { get; } = new List<PolicyDocument>
    {
        new("RP-APP-2.1", "Apparel Resale Policy", "apparel", true,
            "Apparel and clothing such as shirts, jackets, dresses, trousers and knitwear returned in resalable condition with original tags attached are eligible for local resale. Worn, stained or altered garments must be condition-graded before they can be listed."),

        new("RP-FTW-1.4", "Footwear Resale Policy", "footwear", true,
            "Footwear including shoes, sneakers, boots and sandals may be resold locally when returned in like-new condition inside the original box. Visibly used or scuffed footwear is routed to grading before resale."),

        new("RP-ELEC-3.2", "Non-Serialized Electronics Policy", "electronics", true,
            "Non-serialized consumer electronics such as cables, chargers, earbuds and small accessories can be resold after a basic function check. Items powering on and passing inspection are approved for local resale."),

        new("RP-HOME-1.2", "Home and Kitchen Policy", "home", true,
            "Home, kitchen and household merchandise such as cookware, utensils, bedding and decor returned unused in original packaging is eligible for local resale after a visual inspection."),

        new("RP-TOY-1.1", "Toys and Games Policy", "toys", true,
            "Toys, board games and hobby kits returned sealed or with all pieces present are eligible for local resale. Opened items missing components are diverted to clearance instead of full-price resale."),

        new("RP-BOOK-1.0", "Books and Media Policy", "books", true,
            "Books, printed media and unopened recorded media returned in readable undamaged condition are eligible for immediate local resale without further grading."),

        new("RP-GEN-1.0", "General Merchandise Policy", "general", true,
            "General merchandise not covered by a specific category rule and returned in resalable condition may be listed for local resale after standard inspection."),

        new("RP-HYG-1.0", "Personal Care and Hygiene Policy", "hygiene", false,
            "Opened personal-care and hygiene products such as shampoo, deodorant, razors, toothbrushes and skincare cannot be resold once the seal is broken and must be returned to the seller for safety and sanitary reasons."),

        new("RP-COS-1.0", "Cosmetics Policy", "cosmetics", false,
            "Opened cosmetics and makeup including lipstick, foundation, mascara and applied testers are prohibited from resale for hygiene and safety reasons and are returned to the seller."),

        new("RP-FOOD-1.0", "Perishable and Food Policy", "food", false,
            "Food, beverages, perishable and consumable grocery items are prohibited from local resale and are returned to the seller or disposed of per safety rules regardless of packaging state."),

        new("RP-SER-2.0", "Serialized Electronics Policy", "serialized", false,
            "Serialized and warranty-tracked electronics such as phones, laptops and tablets with a registered serial number must return to the seller so warranty and activation records stay intact; local resale is not permitted."),

        new("RP-MED-1.0", "Medical Devices Policy", "medical", false,
            "Medical devices, diagnostic equipment and prescription-linked health products are restricted from resale by retailer policy and are always returned to the seller."),

        new("RP-HAZ-1.0", "Hazardous Materials Policy", "hazardous", false,
            "Hazardous items including loose lithium batteries, aerosols, flammable liquids and pressurized containers are prohibited from local resale and must be routed to the seller through compliant hazmat handling."),
    };

    public static PolicyDocument Default { get; } = new(
        "RP-DEF-0.0",
        "Default Return Policy",
        "unknown",
        false,
        "No matching resale policy was retrieved with sufficient confidence; the item is routed back to the seller pending manual policy review.");
}
