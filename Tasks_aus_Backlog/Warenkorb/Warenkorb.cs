using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Warenkorb
{
    internal static class PossibleProducts
    {
        static internal string[] Get()
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
            Size = size;
            Content = new string[Size];
            Content = Fill(Content);
        }
        public int Find(string productForSearch)
        {
            int i = 0;
            foreach( string product in Content)
            {
                if (product.Trim().ToLower() == productForSearch.Trim().ToLower())
                {
                    return i;
                }
                i++;
            }
            return -1;
        }
        public int Add(string newProduct)
        {
            int freePosNr = 0;
            foreach (string product in Content)
            {
                if (product == "")
                {
                    Content[freePosNr] = newProduct;
                    return freePosNr;
                }
                freePosNr++;
            }
            return -1;
        }
        public int DeleteLast()
        {
            int firstFreePos = 0;
            foreach (string product in Content)
            {
                if (product == "")
                {
                    if (Delete(firstFreePos - 1)) return firstFreePos - 1;
                }
                firstFreePos++;
            }
            if (Delete(firstFreePos)) return firstFreePos;
            return -1;
        }
        public bool Delete(int nr)
        {
            Content[nr] = "";
            return true;
        }
        public bool Sort()
        {
            for (int currentNr = 1; currentNr < Content.Length; currentNr++)
            {
                int sortNr = currentNr - 1;
                while (sortNr >= 0 && String.Compare(Content[sortNr], Content[currentNr], StringComparison.CurrentCultureIgnoreCase) > 0)
                {
                    Content[sortNr + 1] = Content[sortNr];
                    sortNr--;
                }
                Content[sortNr + 1] = Content[currentNr];
            }
            return true;
        }
        internal string[] Fill(string[] arr)
        {
            string[] possibleProducts = PossibleProducts.Get();
            Random random = new Random();
            int posToFill = arr.Length / 3;
            int i;
            for(i = 0; i < posToFill; i++)
            {
                arr[i] = possibleProducts[random.Next(possibleProducts.Length)];
            }
            return arr;
        }
    }
}
