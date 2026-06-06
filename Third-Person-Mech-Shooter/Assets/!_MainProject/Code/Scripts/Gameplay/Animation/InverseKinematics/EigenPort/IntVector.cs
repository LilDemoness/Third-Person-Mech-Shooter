namespace EigenPort
{
    public class IntVector
    {
        private int[] _values;
        private int _size;

        public IntVector(int size)
        {
            _size = size;
            _values = new int[size];
        }

        public int this[int index]
        {
            get => _values[index];
            set => _values[index] = value;
        }

        public void Resize(int newSize)
        {
            _size = newSize;
            _values = new int[newSize];
            SetZeros();
        }
        public int GetSize() => _size;

        public void SetZeros()
        {
            for (int i = 0; i < GetSize(); ++i)
                _values[i] = 0;
        }
    }
}