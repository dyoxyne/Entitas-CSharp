using Entitas;

public class ObjectGetProperty : IPerformanceTest {
    const int n = 10000000;
    Context _context;

    public void Before() {
        _context = new Context(1);
    }

    public void Run() {
        for (int i = 0; i < n; i++) {
            var c = _context.totalComponents;
        }
    }
}
