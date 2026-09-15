# Run with: powershell -NoProfile -File .\Check-Warenkorb.ps1
# Checks the basket model without opening the GUI.
$source = Get-Content (Join-Path $PSScriptRoot 'Warenkorb.cs') -Raw
Add-Type -TypeDefinition ($source + @'
public static class WarenkorbCheck
{
    public static void Run()
    {
        var cart = new Warenkorb.Warenkorb(3);
        cart.Content = new[] { "", "", "" };
        if (cart.DeleteLast() != -1 || cart.Delete(-1) || cart.Delete(3))
            throw new System.Exception("Empty / invalid deletion failed");
        if (cart.Add("  ") != -1 || cart.Add("c") != 0 || cart.Add("a") != 1 || cart.Add("b") != 2 || cart.Add("d") != -1)
            throw new System.Exception("Add / capacity failed");
        cart.Sort();
        if (string.Join(",", cart.Content) != "a,b,c" || cart.Find(" B ") != 1)
            throw new System.Exception("Sort / search failed");
        if (cart.DeleteLast() != 2 || !cart.Delete(0) || cart.DeleteLast() != 1 || cart.DeleteLast() != -1)
            throw new System.Exception("Full / sparse deletion failed");
        cart.Content = new[] { "b", null, "a" };
        cart.Sort();
        if (string.Join(",", cart.Content) != "a,b," || cart.Add("c") != 2)
            throw new System.Exception("Sparse sorting failed");
    }
}
'@)
[WarenkorbCheck]::Run()
Write-Output 'Warenkorb checks passed.'
