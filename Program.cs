namespace Program {
  public class Program {
    public static void Main(string[] args) {
      const int THREADS_TO_TEST = 128;
      var rand = new Random();
      int[] random_work_amounts = new int[THREADS_TO_TEST];
      for (int i=0; i<random_work_amounts.Length; i+=1) {
        random_work_amounts[i] = rand.Next(10, 500);
      }
      Console.WriteLine($"Expected work count sum: {random_work_amounts.Sum().ToString("N0")}");
      var begin_ts = DateTime.Now;
      List<Task> tasks = new List<Task>();
      for (int i=0; i<random_work_amounts.Length; i+=1) {
        tasks.Add(DoWork(random_work_amounts[i]));
      }

      Console.WriteLine($"Spawned work at {RuntimeDuration(begin_ts)}");
      Task.WaitAll(tasks);

      Console.WriteLine($"All threads completed at {RuntimeDuration(begin_ts)}");

      // Now sum recorded work
      Console.WriteLine($"Actual work count sum: {work_done_count.ToString("N0")}");
    }

    public static string RuntimeDuration(DateTime begin) {
      var duration = DateTime.Now - begin;
      return $"{duration.Minutes}:{duration.Seconds}.{duration.Milliseconds}";
    }

    // Multi-threading space

    public static int work_done_count = 0;

    public static async Task DoWork(int num_works_to_do) {
      for (int i=0; i<num_works_to_do; i+=1) {
        await Task.Delay(5);
        work_done_count += 1;
      }
    }


  }
}
