/// <summary>
///The function sums all values ​​from the input array 
///and divides the result by the number of elements to calculate the arithmetic mean.
/// </summary>
/// <param name="a"> numeric onlya array for calculating </param>
/// <returns> Calculated avarage value  </returns>
static double calcAvarageVal(int [ ]a) 
{
    int i = 0;
     double summe = 0;
     double result;
    //Calculated avarage value 
    while (i < a.Length) {
        summe = summe + a[i];
         i++;
         }
    //Claculating of avarage value
    result = summe / a.Length;
    return result;
     }
