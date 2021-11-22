using System;
using System.Collections.Generic;
using System.Linq;

namespace Lottie.Helpers {
    public sealed class PaginationHelper {
        public static IEnumerable<T> PerformPagination<T>(T[] source, int page, out bool firstPage, out bool finalPage, out string pageDescriptor) {
            if (source == null || !source.Any()) {
                firstPage = false;
                finalPage = false;
                pageDescriptor = string.Empty;

                return null;
            }

            int pageStart = page * Lottie.PaginationSize;
            if (pageStart >= source.Length) {
                pageStart = (int)(Math.Floor((source.Length - 1) / (float) Lottie.PaginationSize)) * Lottie.PaginationSize;
            }

            int pageSize = Math.Min(Lottie.PaginationSize, Math.Max(1, source.Length - pageStart));
            firstPage = pageStart == 0;
            finalPage = !(pageStart + pageSize < source.Length);
            pageDescriptor = pageSize == 1 ? "1" : $"{pageStart + 1}-{pageStart + pageSize}";

            return source[pageStart..(pageStart + pageSize)];
        }
    }
}
