int zahl1, zahl2, summe;
summe = 0;
 
Console.Write("Bitte die erste Zahl eingeben: ");
zahl1 = Convert.ToInt32(Console.ReadLine());

Console.Write("Bitte die zweite Zahl eingeben: ");
zahl2 = Convert.ToInt32(Console.ReadLine());

summe = zahl1 + zahl2;
int diferenc = zahl1 - zahl2;
int quotient = zahl1 / zahl2;
int rest = zahl1 % zahl2;
int multiplizieren = zahl1 * zahl2;





Console.WriteLine("Die Summe der zwei Zahlen lautet: {0} ", summe);
Console.WriteLine("Die Differenz der zwei Zahlen lautet: {0} ", diferenc);
Console.WriteLine($"Der Quotient der zwei Zahlen lautet: {quotient} " + (rest!=0 ? $"Rest: {rest}" : "ohne Rest"));
Console.WriteLine("Das Produkt  der zwei Zahlen lautet: {0} ", multiplizieren);


Console.Write("Press any key to continue . . . ");
Console.ReadKey(true);
