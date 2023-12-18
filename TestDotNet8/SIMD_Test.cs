using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Text;
using System.Threading.Tasks;

internal class SIMD_Test
{
    public class NormalCalc
    {
        public static double Multiply(double[] nums)
        {
            double result = 1.0d;

            for (int i = 0; i < nums.Length; i++)
            {
                result *= nums[i];
            }
            return result;
        }

        public static double AddTotal(double[] nums)
        {
            double result = 0.0d;

            for (int i = 0; i < nums.Length; i++)
            {
                result += nums[i];
            }
            return result;
        }
    }

    public unsafe static double Multiply(double[] nums)
    {
        int vectorSize = Vector<double>.Count;
        var accVector = Vector<double>.One;
        int i;
        var array = nums;
        double result = 1.0d;
        fixed (double* p = array)
        {
            for (i = 0; i <= array.Length - vectorSize; i += vectorSize)
            {
                //var v = new Vector<double>(array, i);
                var v = Unsafe.Read<Vector<double>>(p + i);
                accVector = Vector.Multiply(accVector, v);
            }
        }
        var tempArray = new double[Vector<double>.Count];
        accVector.CopyTo(tempArray);
        for (int j = 0; j < tempArray.Length; j++)
        {
            result = result * tempArray[j];
        }

        for (; i < array.Length; i++)
        {
            result *= array[i];
        }

        return result;
    }

    static int Find_Generic_128_(ReadOnlySpan<int> data, int value)
    {
        // In theory we should check for Vector128.IsHardwareAccelerated and dispatch
        // accordingly, in practice here we don't to keep the code simple.
        var vInts = MemoryMarshal.Cast<int, Vector128<int>>(data);

        var compareValue = Vector128.Create(value);
        var vectorLength = Vector128<int>.Count;

        // Batch <4 x int> per loop
        for (var i = 0; i < vInts.Length; i++)
        {
            var result = Vector128.Equals(vInts[i], compareValue);
            if (result == Vector128<int>.Zero)
                continue;

            for (var k = 0; k < vectorLength; k++)
                if (result.GetElement(k) != 0)
                    return i * vectorLength + k;
        }

        // Scalar process of the remaining
        for (var i = vInts.Length * vectorLength; i < data.Length; i++)
            if (data[i] == value)
                return i;

        return -1;
    }

    public static void Test()
    {
        Console.WriteLine($"是否支持SIMD：{Vector.IsHardwareAccelerated}");
        //生成运算数组
        double[] nums = new double[100000];
        Random random = new Random();
        for (int i = 0; i < nums.Length; i++)
        {
            nums[i] = random.NextDouble() * 2.723;
        }

        //普通连乘
        Stopwatch stopwatch = Stopwatch.StartNew();
        for (int i = 0; i < 10000; i++)
        {
            NormalCalc.Multiply(nums);
        }
        stopwatch.Stop();
        Console.WriteLine(stopwatch.ElapsedMilliseconds);

        //Vector
        stopwatch = Stopwatch.StartNew();
        for (int i = 0; i < 10000; i++)
        {
            Multiply(nums);
        }
        stopwatch.Stop();
        Console.WriteLine(stopwatch.ElapsedMilliseconds);
    }
}    

