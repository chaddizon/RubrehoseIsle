using System;
using System.Globalization;

namespace Rubrehose.Core
{
    // Mirrors the K/M/B/T suffix formatting from rubrehose_prototype.html's fmt().
    public static class Format
    {
        private static readonly string[] Units = { "K", "M", "B", "T", "Qa", "Qi" };

        public static string Number(double n)
        {
            if (n < 1000) return Math.Floor(n).ToString(CultureInfo.InvariantCulture);
            int u = -1;
            while (n >= 1000 && u < Units.Length - 1)
            {
                n /= 1000;
                u++;
            }
            return n.ToString("F2", CultureInfo.InvariantCulture) + Units[u];
        }
    }
}
