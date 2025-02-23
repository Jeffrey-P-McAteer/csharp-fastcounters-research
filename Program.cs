namespace Program {
  public class Program {

    public const int THREADS_TO_TEST = 256;
    public const int WORK_AMOUNT_MAX = 900;

    public static void Main(string[] args) {
      var rand = new Random();
      int[] random_work_amounts = new int[THREADS_TO_TEST];
      for (int i=0; i<random_work_amounts.Length; i+=1) {
        random_work_amounts[i] = rand.Next(10, WORK_AMOUNT_MAX);
      }
      Console.WriteLine($"Expected work count sum: {random_work_amounts.Sum().ToString("N0")}");
      int rand_heat_amount = rand.Next(2, 20);
      Console.WriteLine($"rand_heat_amount = {rand_heat_amount}");

      HeatUpTest(typeof(SimpleBrokenInteger), rand_heat_amount, random_work_amounts);
      RunTest(new SimpleBrokenInteger(), random_work_amounts, and_report:true);

      HeatUpTest(typeof(IntegerInterlockedIncrement), rand_heat_amount, random_work_amounts);
      RunTest(new IntegerInterlockedIncrement(), random_work_amounts, and_report:true);

      HeatUpTest(typeof(ConcurrentDictionaryCounter), rand_heat_amount, random_work_amounts);
      RunTest(new ConcurrentDictionaryCounter(), random_work_amounts, and_report:true);

      HeatUpTest(typeof(SimpleIntegerAndSemaphoreSlim), rand_heat_amount, random_work_amounts);
      RunTest(new SimpleIntegerAndSemaphoreSlim(), random_work_amounts, and_report:true);

      // Finally report classes in order of average fastest_time
      Console.WriteLine($"= = = Relative Speed = = =");
      List<string> class_names = new List<string>(all_class_test_durations.Keys);
      class_names = class_names.OrderBy(name => (double) all_class_test_durations[name].Sum(td => td.Ticks) / (double) all_class_test_durations[name].Count() ).ToList();
      int max_name_len = 0;
      foreach (var name in class_names) {
        max_name_len = Math.Max(max_name_len, name.Length);
      }
      foreach (var name in class_names) {
        long avg_ticks = (long) ( (double) all_class_test_durations[name].Sum(td => td.Ticks) / (double) all_class_test_durations[name].Count() );
        long slowest_ticks = all_class_test_durations[name].Max(td => td.Ticks);
        long fastest_ticks = all_class_test_durations[name].Min(td => td.Ticks);
        string padding = "".PadRight((max_name_len+4) - name.Length);
        long spread = slowest_ticks-fastest_ticks;
        Console.WriteLine($"[ {name} ]{padding} averaged {avg_ticks.ToString("N0")} ticks (slowest {slowest_ticks.ToString("N0")} fastest {fastest_ticks.ToString("N0")} spread {spread.ToString("N0")})");
      }

    }

    public static string RuntimeDuration(DateTime begin) {
      var duration = DateTime.Now - begin;
      if (duration.Minutes > 0) {
        return $"{duration.Minutes}:{duration.Seconds}.{duration.Milliseconds}";
      }
      else {
        return $"{duration.Seconds}.{duration.Milliseconds}";
      }
    }

    private static Dictionary<string, List<TimeSpan>> all_class_test_durations = new Dictionary<string, List<TimeSpan>>();

    public static void HeatUpTest(Type t_ws, int rand_heat_amount, int[] work_amounts) {
      if (!all_class_test_durations.ContainsKey(t_ws.Name)) {
        all_class_test_durations[t_ws.Name] = new List<TimeSpan>();
      }
      var begin_ts = DateTime.Now;
      for (int i=0; i<rand_heat_amount; i+=1) {
        object? inst = Activator.CreateInstance(t_ws);
        if (inst is IWorkTrackerStrategy ws) {
          var once_begin_ts = DateTime.Now;
          RunTest(ws, work_amounts, and_report:false);
          all_class_test_durations[t_ws.Name].Add(DateTime.Now - once_begin_ts);
          /*Task.WaitAll(ws.IssueWork(work_amounts));
          string work_done = ws.ReadWorkDoneCount().ToString("N0");
          if (work_done.Length == 0) {
            break; // inserted diff logic to avoid removal of ReadWorkDoneCount call
          }*/
        }
      }
      Console.WriteLine($"[ {t_ws.Name} ] HeatUpTest {rand_heat_amount} took {RuntimeDuration(begin_ts)}");
      Console.WriteLine();
    }

    public static void RunTest(IWorkTrackerStrategy ws, int[] work_amounts, bool and_report=false) {
      string clazz_name = ws.GetType().Name;
      if (!all_class_test_durations.ContainsKey(clazz_name)) {
        all_class_test_durations[clazz_name] = new List<TimeSpan>();
      }
      if (and_report) {
        Console.WriteLine($"[ {clazz_name} ] {ws.GetTestDescription()}");
      }
      var begin_ts = DateTime.Now;
      List<Task> tasks = ws.IssueWork(work_amounts);
      if (and_report) {
        Console.WriteLine($"[ {clazz_name} ] IssueWork completed at {RuntimeDuration(begin_ts)}");
      }
      Task.WaitAll(tasks);
      if (and_report) {
        Console.WriteLine($"[ {clazz_name} ] All threads completed at {RuntimeDuration(begin_ts)}");
        all_class_test_durations[clazz_name].Add(DateTime.Now - begin_ts);
        int actual_work_count = ws.ReadWorkDoneCount();
        Console.WriteLine($"[ {clazz_name} ] Actual work count sum: {actual_work_count.ToString("N0")}");
        if (actual_work_count == work_amounts.Sum()) {
          Console.WriteLine($"[ {clazz_name} ] Passes and WORKS CORRECTLY");
        }
        else {
          Console.WriteLine($"[ {clazz_name} ] Cannot be used and is BROKEN");
        }
        Console.WriteLine();
      }
    }

  }

  public interface IWorkTrackerStrategy {
    public string GetTestDescription();
    public List<Task> IssueWork(int[] work_amounts);
    public int ReadWorkDoneCount();
  }

  public class WorkProvider {
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CS1998:This async method lacks 'await' operators and will run synchronously.", Justification = "This code is supposed to run synchronously.")]
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoOptimization | System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    public static async Task DoOneUnitOfWork() {
      double sum = 1.0;
      for (int i=0; i<200; i+=1) {
        if (i % 5 == 3) {
          sum -= (double) i;
        }
        else if (i % 3 == 1) {
          sum += (double) i;
        }
        else if (i % 2 == 0) {
          sum *= (double) i;
          if (sum > 0.0) {
            sum -= 1;
          }
          else {
            sum += 1;
          }
        }
        else {
          if (sum < 0.0) {
            sum += 3.14; // Give it some extra pie
          }
          sum += 3.14;
        }
        if (Math.Abs((int) sum) % 25 == 5) { // Statistically is hit twice per each call to DoOneUnitOfWork
          await Task.Delay(1);
        }
      }
    }
  }

  public class SimpleBrokenInteger: IWorkTrackerStrategy {
    public string GetTestDescription() { return "Keeps an int around and directly uses += 1 to increment after work is done."; }

    public List<Task> IssueWork(int[] work_amounts) {
      List<Task> tasks = new List<Task>();
      for (int i=0; i<work_amounts.Length; i+=1) {
        tasks.Add(this.DoWork(work_amounts[i]));
      }
      return tasks;
    }

    public int ReadWorkDoneCount() {
      return work_done_count;
    }

    private int work_done_count = 0;

    private async Task DoWork(int num_works_to_do) {
      for (int i=0; i<num_works_to_do; i+=1) {
        await WorkProvider.DoOneUnitOfWork();
        work_done_count += 1;
      }
    }
  }

  public class IntegerInterlockedIncrement: IWorkTrackerStrategy {
    public string GetTestDescription() { return "Keeps an int around and calls Interlocked.Increment to increment after work is done."; }

    public List<Task> IssueWork(int[] work_amounts) {
      List<Task> tasks = new List<Task>();
      for (int i=0; i<work_amounts.Length; i+=1) {
        tasks.Add(this.DoWork(work_amounts[i]));
      }
      return tasks;
    }

    public int ReadWorkDoneCount() {
      long num1 = Interlocked.Read(ref work_done_count);
      long num2 = Interlocked.Read(ref work_done_count);
      while (num1 != num2) {
        num1 = Interlocked.Read(ref work_done_count);
        num2 = Interlocked.Read(ref work_done_count);
      }
      return (int) num1;
    }

    private long work_done_count = 0;

    private async Task DoWork(int num_works_to_do) {
      for (int i=0; i<num_works_to_do; i+=1) {
        await WorkProvider.DoOneUnitOfWork();
        Interlocked.Increment(ref work_done_count);
      }
    }
  }

  public class ConcurrentDictionaryCounter: IWorkTrackerStrategy {
    public string GetTestDescription() { return "Keeps ConcurrentDictionary<string, int> around increments the string version of num_works_to_do by one for each work done."; }

    public List<Task> IssueWork(int[] work_amounts) {
      List<Task> tasks = new List<Task>();
      for (int i=0; i<work_amounts.Length; i+=1) {
        tasks.Add(this.DoWork(work_amounts[i]));
      }
      return tasks;
    }

    public int ReadWorkDoneCount() {
      int sum = 0;
      foreach (var (k,v) in this.work_tracker) {
        sum += v;
      }
      return sum;
    }

    private System.Collections.Concurrent.ConcurrentDictionary<string, int> work_tracker = new System.Collections.Concurrent.ConcurrentDictionary<string, int>();

    private async Task DoWork(int num_works_to_do) {
      string k = ""+num_works_to_do;
      if (!work_tracker.ContainsKey(k)) {
        work_tracker.AddOrUpdate(k, 0, (key, old_val) => 0);
      }
      for (int i=0; i<num_works_to_do; i+=1) {
        await WorkProvider.DoOneUnitOfWork();

        for (int j=0; j<16; j+=1) {
          if (work_tracker.TryGetValue(k, out int work_count)) {
            if (work_tracker.TryUpdate(k, work_count + 1, work_count)) {
              break;
            }
          }
        }

      }
    }
  }

  public class SimpleIntegerAndSemaphoreSlim: IWorkTrackerStrategy {
    public string GetTestDescription() { return "Keeps an int around and directly uses += 1 to increment after work is done within a guard provided by SemaphoreSlim with 1 thread available."; }

    public List<Task> IssueWork(int[] work_amounts) {
      List<Task> tasks = new List<Task>();
      for (int i=0; i<work_amounts.Length; i+=1) {
        tasks.Add(this.DoWork(work_amounts[i]));
      }
      return tasks;
    }

    public int ReadWorkDoneCount() {
      int local_wdc = 0;
      semaphore.Wait();
      local_wdc = work_done_count;
      semaphore.Release();
      return local_wdc;
    }

    private SemaphoreSlim semaphore = new SemaphoreSlim(1);
    private int work_done_count = 0;

    private async Task DoWork(int num_works_to_do) {
      for (int i=0; i<num_works_to_do; i+=1) {
        await WorkProvider.DoOneUnitOfWork();

        semaphore.Wait();
        work_done_count += 1;
        semaphore.Release();

      }
    }
  }


}
