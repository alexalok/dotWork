using System;

namespace dotWork.Tests.TestExceptions
{
    class NthIterationExecutedException : Exception
    {
        public int IterationNumber { get; }

        public NthIterationExecutedException(int iterationNumber)
        {
            IterationNumber = iterationNumber;
        }
    }
}
