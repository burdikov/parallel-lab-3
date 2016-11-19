using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Prlel_LAB_3
{
    class Program
    {
        // Общий буфер
        static string buffer = "";
        // Его статус
        static bool isEmpty = true;
        // Флаг окончания работы писателей
        static bool isFinished = false;

        
        /// <summary>Количество потоков-писателей</summary>
        const int N = 4;
        /// <summary>Количество потоков-читателей</summary>
        const int M = 4;
                        
        /// <summary>Количество сообщений</summary>
        const int X = 10000;

        /// <summary>Массив массивов сообщений</summary>
        static string[][] messages = new string[N][];

        // Приемник сообщений
        static List<string>[] vault = new List<string>[]
      {
        new List<string>(N*X),
        new List<string>(N*X),
        new List<string>(N*X),
        new List<string>(N*X),
        new List<string>(N*X)
      };

        static void Main(string[] args)
        {
            // Инициализация массива сообщений
            for (int i = 0; i < N; i++)
            {
                messages[i] = new string[X];
                for (int j = 0; j < X; j++)
                {
                    messages[i][j] = i + "" + j;
                }
            }

            // Тестовая часть
            V1();
            V2();
            V3();
            V4();
            V5();
            
            Console.ReadKey();
        }

        #region Отсутствие синхронизации

        static void V1_Read()
        {
            while (!isFinished)
            {
                if (!isEmpty)
                {
                    vault[0].Add(buffer);
                    isEmpty = true;
                }
            }
        }

        static void V1_Write(object o)
        {
            int myNumber = (int)o;
            int i = 0;
            while (i < X)
            {
                if (isEmpty)
                {
                    buffer = messages[myNumber][i++];
                    isEmpty = false;
                }
            }
        }

        static void V1()
        {
            Thread[] writers = new Thread[N];
            Thread[] readers = new Thread[M];

            Stopwatch timer = new Stopwatch();

            isFinished = false;

            timer.Start();
            for (int i = 0; i < M; i++)
            {
                readers[i] = new Thread(V1_Read);
                readers[i].Start();
            }
            for (int i = 0; i < N; i++)
            {
                writers[i] = new Thread(V1_Write);
                writers[i].Start(i);
            }
            for (int i = 0; i < N; i++)
            {
                writers[i].Join();
            }
            isFinished = true;
            for (int i = 0; i < M; i++)
            {
                readers[i].Join();
            }
            timer.Stop();

            Console.WriteLine("Время: " + timer.ElapsedMilliseconds);
        }
        #endregion

        #region Lock
        static void V2_Write(object o)
        {
            int myNumber = (int)o;
            int i = 0;
            while (i < X)
            {
                lock ("WRITE")
                  if (isEmpty)
                    {
                        buffer = messages[myNumber][i++];
                        isEmpty = false;
                    }
            }
        }

        static void V2_Read()
        {
            while (!isFinished)
            {
                if (!isEmpty)
                    lock ("READ")
                      if (!isEmpty)
                        {
                            vault[1].Add(buffer);
                            isEmpty = true;
                        }
            }
        }

        static void V2()
        {
            Thread[] writers = new Thread[N];
            Thread[] readers = new Thread[M];

            Stopwatch timer = new Stopwatch();

            isFinished = false;

            timer.Start();
            for (int i = 0; i < M; i++)
            {
                readers[i] = new Thread(V2_Read);
                readers[i].Start();
            }
            for (int i = 0; i < N; i++)
            {
                writers[i] = new Thread(V2_Write);
                writers[i].Start(i);
            }
            for (int i = 0; i < N; i++)
            {
                writers[i].Join();
            }
            isFinished = true;
            for (int i = 0; i < M; i++)
            {
                readers[i].Join();
            }
            timer.Stop();

            Console.WriteLine("Время: " + timer.ElapsedMilliseconds);
        }
        #endregion

        #region AutoResetEvent
        static AutoResetEvent eventFull = new AutoResetEvent(false);
        static AutoResetEvent eventEmpty = new AutoResetEvent(true);

        static void V3_Read()
        {
            while (eventFull.WaitOne())
            {
                if (isFinished)
                {
                    eventFull.Set();
                    break;
                }
                vault[2].Add(buffer);
                eventEmpty.Set();
            }
        }

        static void V3_Write(object o)
        {
            int myNumber = (int)o;
            int i = 0;

            while ((i < X) && eventEmpty.WaitOne())
            {
                buffer = messages[myNumber][i++];
                eventFull.Set();
            }
        }

        static void V3()
        {
            Thread[] writers = new Thread[N];
            Thread[] readers = new Thread[M];

            Stopwatch timer = new Stopwatch();

            isFinished = false;

            timer.Start();
            for (int i = 0; i < M; i++)
            {
                readers[i] = new Thread(V3_Read);
                readers[i].Start();
            }
            for (int i = 0; i < N; i++)
            {
                writers[i] = new Thread(V3_Write);
                writers[i].Start(i);
            }
            for (int i = 0; i < N; i++)
            {
                writers[i].Join();
            }
            isFinished = true;
            eventFull.Set();

            for (int i = 0; i < M; i++)
            {
                readers[i].Join();
            }
            timer.Stop();

            Console.WriteLine("Время: " + timer.ElapsedMilliseconds);
        }
        #endregion

        #region Semaphore
        static Semaphore semEmpty = new Semaphore(1,1);
        static Semaphore semFull = new Semaphore(0,1);

        static void V4_Write(object o)
        {
            int myNumber = (int)o;
            int i = 0;
            while (i < X && semEmpty.WaitOne())
            {
                buffer = messages[myNumber][i++];
                semFull.Release();
            }
        }

        static void V4_Read()
        {
            while (semFull.WaitOne())
            {
                if (isFinished)
                {
                    semFull.Release();
                    break;
                }
                vault[3].Add(buffer);
                semEmpty.Release();
            }
        }

        static void V4()
        {
            Thread[] writers = new Thread[N];
            Thread[] readers = new Thread[M];

            Stopwatch timer = new Stopwatch();

            isFinished = false;

            timer.Start();
            for (int i = 0; i < M; i++)
            {
                readers[i] = new Thread(V4_Read);
                readers[i].Start();
            }
            for (int i = 0; i < N; i++)
            {
                writers[i] = new Thread(V4_Write);
                writers[i].Start(i);
            }
            for (int i = 0; i < N; i++)
            {
                writers[i].Join();
            }

            isFinished = true;

            semFull.Release();

            for (int i = 0; i < M; i++)
            {
                readers[i].Join();
            }
            timer.Stop();

            Console.WriteLine("Время: " + timer.ElapsedMilliseconds);
        }
        #endregion

        #region Interlocked
        static int allowREAD = 0;
        static int allowWRITE = 1;

        static void V5()
        {
            Thread[] writers = new Thread[N];
            Thread[] readers = new Thread[M];

            Stopwatch timer = new Stopwatch();

            isFinished = false;

            timer.Start();
            for (int i = 0; i < M; i++)
            {
                readers[i] = new Thread(V5_Read);
                readers[i].Start();
            }
            for (int i = 0; i < N; i++)
            {
                writers[i] = new Thread(V5_Write);
                writers[i].Start(i);
            }
            for (int i = 0; i < N; i++)
            {
                writers[i].Join();
            }

            isFinished = true;

            for (int i = 0; i < M; i++)
            {
                readers[i].Join();
            }
            timer.Stop();

            Console.WriteLine("Время: " + timer.ElapsedMilliseconds);
        }

        static void V5_Read()
        {
            while (!isFinished)
            {
                if (Interlocked.CompareExchange(ref allowREAD, 0, 1) == 1)
                {
                    vault[4].Add(buffer);
                    allowWRITE = 1;
                }
            }
        }

        static void V5_Write(object o)
        {
            int myNumber = (int)o;
            int i = 0;
            while (i < X)
            {
                if (Interlocked.CompareExchange(ref allowWRITE, 0, 1) == 1)
                {
                    buffer = messages[myNumber][i++];
                    allowREAD = 1;
                }
            }
        }
        #endregion
    }
}
