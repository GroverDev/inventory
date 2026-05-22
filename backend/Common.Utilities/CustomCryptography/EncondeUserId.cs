using System;
using Sqids;

namespace Common.Utilities.CustomCryptography;

public class EncondeUserId
{
    private static readonly string alphabet = "GTHivO7BCtekZdJ4DYoFPasXIqyzhx3WMLQERAbr1f586SwjNnK2lgpcUVu0m9";
    private static readonly int minLength = 7;
    public static string EncodeId(int id)
    {
        var sqids = new SqidsEncoder<int>(new()
        {
            // This is a shuffled version of the default alphabet, which includes lowercase letters (a-z), uppercase letters (A-Z), and digits (0-9)
            Alphabet = alphabet,
            MinLength = minLength
        });
        return sqids.Encode(id);
    }
    public static int DecodeId(string encodedId)
    {
        var sqids = new SqidsEncoder<int>(new()
        {
            // This is a shuffled version of the default alphabet, which includes lowercase letters (a-z), uppercase letters (A-Z), and digits (0-9)
            Alphabet = alphabet,
            MinLength = minLength
        });
        if (sqids.Decode(encodedId) is [var singleNumber])
        {
            return singleNumber > 0 ? singleNumber : 0;
        }
        return 0;
    }
}
