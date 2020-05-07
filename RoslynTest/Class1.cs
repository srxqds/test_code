using System;

namespace RoslynTest
{
    public class Class1
    {
        public int instanceV;

        public void test()
        {
            instanceV = 1;
        }
        public static readonly int[] IntArray = new int[] { 1,2,};

        public readonly int[] IntArray1 = new int[] { 1, 2 };
        
        public static void TestPass(int[] intArray)
        {
            intArray[1] = 2;
        }

        public static void TestSecond(int[] intArray)
        {
            int[] temp = IntArray;
            temp = IntArray;
            temp[1] = 2;
            int[] temp1 = new int[] { 1};
            temp1[0] = 3;
            GetIntArray()[2] = 3;
        }

        public static int[] GetIntArray()
        {
            return IntArray;
        }

        public static void TestOutPass(out int[] intArray)
        {
            intArray = new int[] { };
        }

        public static void TestMain()
        {
            IntArray[1] = 5;
            TestPass(IntArray);
            TestSecond(IntArray);
        }
    }
}
