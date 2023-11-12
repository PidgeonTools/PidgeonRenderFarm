public class ProgressBar
{
    private float Start { get; set; }
    private float End { get; set; }
    public float Current { get; set; }
    public float Step { get; set; }

    public string Info_Text;
    public char Border_Char = '|';
    public char Progress_Char = '#';
    public char Empty_Char = ' ';

    public ProgressBar(float end, string info_text = "", float start = 0, float step = 1)
    {
        Start = start / 1.0f;
        End = end / 1.0f;
        Step = step / 1.0f;
        Info_Text = info_text;
    }

    public void Show()
    {
        Console.WriteLine(Get());
    }
    public void Show(float step)
    {
        Update(step / 1.0f);
        Console.WriteLine(Get());
    }

    public string Get()
    {
        float percent = Current / (End - Start) * 100;
        string percent_string = percent.ToString("##0");
        int bar_max = Console.WindowWidth - 6 - Info_Text.Length;
        int bar_factor = (int)Math.Floor(bar_max / (End - Start));

        string bar = Info_Text;
        bar += percent_string.PadLeft(3, ' ') + "%";
        bar += Border_Char;
        bar += string.Concat(Enumerable.Repeat(Progress_Char, (int)Math.Round(Current * bar_factor)));
        bar += "".PadRight((int)(End - Current - Start) * bar_factor, Empty_Char);
        bar += Border_Char;

        return bar;
    }

    public void Update(float step = 0)
    {
        if (step == 0)
        {
            step = Step;
        }
        Current += step;
        Current = Math.Clamp(Current, Start, End);
    }
}