using System;

namespace Warenkorb
{
    internal static class PossibleProducts
    {
        internal static string[] Get()
        {
            return new[] { "a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k" };
        }
    }

    internal class Warenkorb
    {
        public int Size { get; private set; }
        public string[] Content;

        public Warenkorb(int size)
        {
            if (size < 0) throw new ArgumentOutOfRangeException(nameof(size));
            Size = size;
            Content = Fill(new string[size]);
        }

        public int Find(string productForSearch)
        {
            if (string.IsNullOrWhiteSpace(productForSearch)) return -1;
            return Array.FindIndex(Content, product => string.Equals(product == null ? null : product.Trim(),
                productForSearch.Trim(), StringComparison.CurrentCultureIgnoreCase));
        }

        public int Add(string newProduct)
        {
            if (string.IsNullOrWhiteSpace(newProduct)) return -1;
            int index = Array.FindIndex(Content, string.IsNullOrEmpty);
            if (index >= 0) Content[index] = newProduct.Trim();
            return index;
        }

        public int DeleteLast()
        {
            int index = Array.FindLastIndex(Content, product => !string.IsNullOrEmpty(product));
            if (index >= 0) Delete(index);
            return index;
        }

        public bool Delete(int nr)
        {
            if (nr < 0 || nr >= Content.Length || string.IsNullOrEmpty(Content[nr])) return false;
            Content[nr] = "";
            return true;
        }

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
