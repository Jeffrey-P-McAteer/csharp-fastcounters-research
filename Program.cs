namespace Program {
  public class Program {

    public const int THREADS_TO_TEST = 128;
    public const int WORK_AMOUNT_MS = 2;
    public const int WORK_AMOUNT_MAX = 300;

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

    public static void HeatUpTest(Type t_ws, int rand_heat_amount, int[] work_amounts) {
      var begin_ts = DateTime.Now;
      for (int i=0; i<rand_heat_amount; i+=1) {
        object inst = Activator.CreateInstance(t_ws);
        if (inst is IWorkTrackerStrategy ws) {
          RunTest(ws, work_amounts, and_report:false);
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
        Console.WriteLine($"[ {clazz_name} ] Actual work count sum: {ws.ReadWorkDoneCount().ToString("N0")}");
        Console.WriteLine();
      }
    }

  }

  public interface IWorkTrackerStrategy {
    public string GetTestDescription();
    public List<Task> IssueWork(int[] work_amounts);
    public int ReadWorkDoneCount();
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

    public int work_done_count = 0;

    public async Task DoWork(int num_works_to_do) {
      for (int i=0; i<num_works_to_do; i+=1) {
        await Task.Delay(Program.WORK_AMOUNT_MS);
        work_done_count += 1;
      }
    }
  }

}
