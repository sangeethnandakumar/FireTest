namespace FireTest
{
    public class TestResult
    {
        public int Iteration { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int HTTPStatus { get; set; }
        public long ResponseTime { get; set; }
    }
}
