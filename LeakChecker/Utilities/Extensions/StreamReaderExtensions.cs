using System.Buffers;

namespace LeakChecker.Utilities.Extensions;

public static class StreamReaderExtensions
{
    /// <summary>
    /// Reads a line from the current stream asynchronously, 
    /// including the newline characters (\r, \n, or \r\n) if present.
    /// </summary>
    public static async ValueTask<string?> ReadLineWithEndingAsync(this StreamReader reader)
    {
        if (reader is null)
            throw new ArgumentNullException(nameof(reader));

        if (reader.EndOfStream)
            return null;

        char[]? rentedBuffer = null;
        int rentedPos = 0;

        var singleChar = new char[1];

        while (true)
        {
            int read = await reader.ReadAsync(singleChar, 0, 1).ConfigureAwait(false);
            if (read == 0)
                break;

            char ch = singleChar[0];

            // Grow buffer if needed
            if (rentedBuffer is null)
            {
                rentedBuffer = ArrayPool<char>.Shared.Rent(128);
            }
            else if (rentedPos == rentedBuffer.Length)
            {
                char[] newBuffer = ArrayPool<char>.Shared.Rent(rentedBuffer.Length * 2);
                rentedBuffer.AsSpan(0, rentedPos).CopyTo(newBuffer);
                ArrayPool<char>.Shared.Return(rentedBuffer);
                rentedBuffer = newBuffer;
            }

            rentedBuffer[rentedPos++] = ch;

            // Detect newline
            if (ch == '\n')
            {
                string str = new string(rentedBuffer, 0, rentedPos);
                ArrayPool<char>.Shared.Return(rentedBuffer);
                return str;
            }

            if (ch == '\r')
            {
                // Peek ahead for '\n'
                if (reader.Peek() == '\n')
                {
                    read = await reader.ReadAsync(singleChar, 0, 1).ConfigureAwait(false);
                    if (read > 0)
                    {
                        if (rentedPos == rentedBuffer.Length)
                        {
                            char[] newBuffer = ArrayPool<char>.Shared.Rent(rentedBuffer.Length * 2);
                            rentedBuffer.AsSpan(0, rentedPos).CopyTo(newBuffer);
                            ArrayPool<char>.Shared.Return(rentedBuffer);
                            rentedBuffer = newBuffer;
                        }
                        rentedBuffer[rentedPos++] = singleChar[0];
                    }
                }

                string str = new string(rentedBuffer, 0, rentedPos);
                ArrayPool<char>.Shared.Return(rentedBuffer);
                return str;
            }
        }

        if (rentedBuffer is not null)
        {
            string str = new string(rentedBuffer, 0, rentedPos);
            ArrayPool<char>.Shared.Return(rentedBuffer);
            return str;
        }

        return null;
    }
}