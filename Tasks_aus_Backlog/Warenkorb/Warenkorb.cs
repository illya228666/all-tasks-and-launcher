using System;

namespace Warenkorb
{
    /// <summary>
    /// Stellt die verfügbaren Beispielprodukte für die zufällige Erstbefüllung
    /// eines Warenkorbs und für die Anzeige des Sortiments bereit.
    /// </summary>
    /// <remarks>
    /// Produkte werden ausschließlich durch ihre deutschen Bezeichnungen dargestellt.
    /// Preise, Mengen und Artikelnummern gehören nicht zu diesem Datenmodell.
    /// </remarks>
    internal static class PossibleProducts
    {
        /// <summary>
        /// Liefert die festgelegte Auswahl kurzer deutscher Produktnamen.
        /// </summary>
        /// <returns>
        /// Ein neues Array mit elf Produktbezeichnungen. Änderungen am zurückgegebenen
        /// Array beeinflussen spätere Aufrufe dieser Methode nicht.
        /// </returns>
        internal static string[] Get()
        {
            return new[] { "Brot", "Milch", "Käse", "Eier", "Butter", "Apfel", "Reis", "Nudeln", "Tee", "Kaffee", "Wasser" };
        }
    }

    /// <summary>
    /// Verwaltet Produktbezeichnungen in einem Array mit begrenzter Kapazität.
    /// Unterstützt Suchen, Hinzufügen, Entfernen und alphabetisches Sortieren.
    /// </summary>
    /// <remarks>
    /// Jeder belegte Arrayplatz entspricht einem Artikel; gleiche Bezeichnungen dürfen
    /// mehrfach vorkommen. Ein leerer String oder <see langword="null"/> kennzeichnet
    /// einen freien Platz. Alle Positionsangaben sind nullbasiert.
    /// Die Methoden verändern den vorhandenen Warenkorb direkt und verwenden kein LINQ.
    /// Voraussetzung ist, dass <see cref="Content"/> weiterhin auf ein gültiges Array
    /// mit der bei der Erstellung festgelegten Länge verweist.
    /// </remarks>
    internal class Warenkorb
    {
        /// <summary>
        /// Gibt die bei der Erstellung festgelegte maximale Anzahl der Arrayplätze an.
        /// </summary>
        /// <value>
        /// Die Kapazität des Warenkorbs, nicht die Anzahl der aktuell enthaltenen Artikel.
        /// Der Wert kann außerhalb dieser Klasse nicht gesetzt werden.
        /// </value>
        public int Size { get; private set; }
        /// <summary>
        /// Enthält die Produktbezeichnungen und die freien Plätze des Warenkorbs.
        /// </summary>
        /// <remarks>
        /// Das Feld ist direkt zugänglich. Beim externen Ändern müssen die Bedeutung
        /// freier Plätze und die ursprüngliche Arraylänge erhalten bleiben.
        /// Das Ersetzen durch <see langword="null"/> verletzt die Voraussetzung der Methoden.
        /// </remarks>
        public string[] Content;

        /// <summary>
        /// Erstellt einen Warenkorb und befüllt das erste Drittel mit Beispielprodukten.
        /// </summary>
        /// <param name="size">
        /// Gewünschte Kapazität. Null ist erlaubt und erzeugt ein leeres Array.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="size"/> ist kleiner als null.
        /// </exception>
        /// <remarks>
        /// Die Anzahl der Beispielartikel ergibt sich durch ganzzahlige Division durch drei.
        /// Bei zehn Plätzen werden drei Artikel eingetragen; die übrigen Plätze bleiben frei.
        /// </remarks>
        public Warenkorb(int size)
        {
            if (size < 0) throw new ArgumentOutOfRangeException(nameof(size));
            Size = size;
            Content = Fill(new string[size]);
        }

        /// <summary>
        /// Sucht das erste Vorkommen einer Produktbezeichnung im Warenkorb.
        /// </summary>
        /// <param name="productForSearch">Die vollständig gesuchte Produktbezeichnung.</param>
        /// <returns>
        /// Den nullbasierten Index des ersten Treffers oder -1, wenn kein Treffer existiert
        /// oder die Eingabe null, leer oder ausschließlich aus Leerraum besteht.
        /// </returns>
        /// <remarks>
        /// Führender und nachfolgender Leerraum wird beim Vergleich ignoriert.
        /// Der Vergleich berücksichtigt die aktuelle Kultur, jedoch nicht die Groß- und
        /// Kleinschreibung. Es handelt sich um eine exakte Suche, nicht um eine Teiltextsuche.
        /// Der Inhalt des Warenkorbs wird nicht verändert. Laufzeit: O(n).
        /// </remarks>
        public int Find(string productForSearch)
        {
            if (string.IsNullOrWhiteSpace(productForSearch)) return -1;
            return Array.FindIndex(Content, product => string.Equals(product == null ? null : product.Trim(),
                productForSearch.Trim(), StringComparison.CurrentCultureIgnoreCase));
        }

        /// <summary>
        /// Fügt eine Produktbezeichnung am ersten freien Arrayplatz ein.
        /// </summary>
        /// <param name="newProduct">Die einzufügende, nicht leere Produktbezeichnung.</param>
        /// <returns>
        /// Den nullbasierten Index des belegten Platzes oder -1 bei ungültiger Eingabe
        /// beziehungsweise vollständig belegtem Warenkorb.
        /// </returns>
        /// <remarks>
        /// Führender und nachfolgender Leerraum wird vor dem Speichern entfernt.
        /// Doppelte Bezeichnungen sind erlaubt. Lücken durch frühere Löschungen werden
        /// zuerst wiederverwendet; andere Artikel werden nicht verschoben.
        /// Bei einem Fehlschlag bleibt der Inhalt unverändert. Laufzeit: O(n).
        /// </remarks>
        public int Add(string newProduct)
        {
            if (string.IsNullOrWhiteSpace(newProduct)) return -1;
            int index = Array.FindIndex(Content, string.IsNullOrEmpty);
            if (index >= 0) Content[index] = newProduct.Trim();
            return index;
        }

        /// <summary>
        /// Entfernt den Artikel am höchsten belegten Arrayindex.
        /// </summary>
        /// <returns>
        /// Den nullbasierten Index des entfernten Artikels oder -1, wenn kein Artikel vorhanden ist.
        /// </returns>
        /// <remarks>
        /// Die Suche erfolgt vom Arrayende aus und überspringt freie Plätze.
        /// Gemeint ist der letzte Artikel in der aktuellen Arrayreihenfolge, nicht zwingend
        /// der zuletzt hinzugefügte Artikel. Die eigentliche Löschung übernimmt <see cref="Delete"/>.
        /// Laufzeit: O(n).
        /// </remarks>
        public int DeleteLast()
        {
            int index = Array.FindLastIndex(Content, product => !string.IsNullOrEmpty(product));
            if (index >= 0) Delete(index);
            return index;
        }

        /// <summary>
        /// Entfernt den Artikel an einer bestimmten Arrayposition.
        /// </summary>
        /// <param name="nr">Nullbasierter Index des zu entfernenden Artikels.</param>
        /// <returns>
        /// <see langword="true"/>, wenn ein belegter Platz geleert wurde;
        /// andernfalls <see langword="false"/> bei ungültigem Index oder bereits freiem Platz.
        /// </returns>
        /// <remarks>
        /// Die Position wird auf einen leeren String gesetzt. Andere Artikel behalten ihre
        /// Indizes; die entstehende Lücke wird nicht geschlossen. Laufzeit: O(1).
        /// </remarks>
        public bool Delete(int nr)
        {
            if (nr < 0 || nr >= Content.Length || string.IsNullOrEmpty(Content[nr])) return false;
            Content[nr] = "";
            return true;
        }

        /// <summary>
        /// Ordnet die enthaltenen Artikel alphabetisch aufsteigend und verschiebt freie Plätze ans Ende.
        /// </summary>
        /// <returns><see langword="true"/> nach Abschluss, auch bei einem leeren Warenkorb.</returns>
        /// <remarks>
        /// Zunächst werden belegte Plätze unter Beibehaltung ihrer Reihenfolge nach vorne
        /// verschoben. Anschließend wird der belegte Bereich durch Einfügesortierung sortiert:
        /// Der aktuelle Artikel wird zwischengespeichert, größere Vorgänger werden nach rechts
        /// verschoben und der Artikel wird an der frei gewordenen Position eingesetzt.
        /// Der Vergleich erfolgt gemäß der aktuellen Kultur ohne Beachtung der Groß- und
        /// Kleinschreibung. Gleichwertige Artikel behalten ihre relative Reihenfolge.
        /// Das vorhandene Array wird weiterverwendet; seine Länge bleibt unverändert.
        /// Laufzeit im ungünstigsten Fall: O(n²); zusätzlicher Speicherbedarf: O(1).
        /// </remarks>
        public bool Sort()
        {
            int count = 0;
            for (int i = 0; i < Content.Length; i++)
            {
                if (!string.IsNullOrEmpty(Content[i])) Content[count++] = Content[i];
            }
            for (int i = count; i < Content.Length; i++) Content[i] = "";

            for (int i = 1; i < count; i++)
            {
                string product = Content[i];
                int j = i - 1;
                while (j >= 0 && string.Compare(Content[j], product, StringComparison.CurrentCultureIgnoreCase) > 0)
                {
                    Content[j + 1] = Content[j];
                    j--;
                }
                Content[j + 1] = product;
            }
            return true;
        }

        /// <summary>
        /// Überschreibt ein Array mit zufälligen Beispielartikeln und freien Plätzen.
        /// </summary>
        /// <param name="arr">Das zu befüllende Array; darf nicht null sein.</param>
        /// <returns>Dieselbe Arrayinstanz, die als <paramref name="arr"/> übergeben wurde.</returns>
        /// <exception cref="NullReferenceException">
        /// <paramref name="arr"/> ist null; die Methode prüft diese interne Voraussetzung nicht gesondert.
        /// </exception>
        /// <remarks>
        /// Genau arr.Length / 3 Plätze werden mit zufälligen Einträgen aus
        /// <see cref="PossibleProducts.Get"/> belegt. Durch die ganzzahlige Division wird abgerundet.
        /// Mehrfach vorkommende Produkte sind möglich. Alle restlichen Plätze werden auf
        /// leere Strings gesetzt; vorhandene Inhalte werden überschrieben.
        /// Bei weniger als drei Plätzen wird kein Beispielartikel eingetragen. Laufzeit: O(n).
        /// </remarks>
        internal string[] Fill(string[] arr)
        {
            var products = PossibleProducts.Get();
            var random = new Random();
            for (int i = 0; i < arr.Length; i++)
                arr[i] = i < arr.Length / 3 ? products[random.Next(products.Length)] : "";
            return arr;
        }
    }
}
