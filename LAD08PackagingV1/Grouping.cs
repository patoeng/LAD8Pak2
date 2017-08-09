
using System;

namespace LAD08PackagingV1
{
    public delegate void GroupingAchieved();
    public delegate void GroupingValue(int data);

    public delegate void GroupingException(string exception);
    public class Grouping
    {
        public event GroupingAchieved GroupingIsAchievedTarget;
        public event GroupingValue RemainingValueChanged;
        public event GroupingValue PackedValueChanged;
        public event GroupingException GroupingException;
  
        public Grouping(int size)
        {
            Size = size;          
        }

        private readonly bool _withIndividualBox;
        public int BoxQuantity { get; protected set; }
        public Grouping(int size, int initialBoxQuantity, int initialIndividualPacked, bool withIndividualBox)
        {
            Size = size;
            BoxQuantity = initialBoxQuantity;
            _packed = BoxQuantity* Size;
            IndividualPacking = initialIndividualPacked;
            Remaining = Size -  _packed % Size;
            _withIndividualBox = withIndividualBox;
        }
        public int Size { get; protected set; }
        private int _remaining;
        public int Remaining
        {
            get { return _remaining; }
            protected set
            {
                if (_remaining == value) return;
                _remaining = value;
                RemainingValueChanged?.Invoke(_remaining);
            }
        }

        private int _packed;
        public int PrintPending { get; protected set; }

        public bool PendingDoPrint()
        {
            if (PrintPending > 0)
            {
                PrintPending -= 1;
                return true;
            }
            return false;
        }
        public int Packed
        {
            get { return _packed; }
            set
            {
                if (_packed == value) return;
                _packed = value;
                PackedValueChanged?.Invoke(_packed);
                if (value > IndividualPacking && _withIndividualBox)
                {
                    GroupingException?.Invoke("Jumlah quantity produk di Carton Box lebih banyak dari individual box");
                    return;
                }

                Remaining =Size -( _packed % Size);
                var tempBox = Math.Floor((decimal)_packed / Size);
                if (Remaining == Size && tempBox - BoxQuantity==1)
                {
                    PrintPending += 1;
                    BoxQuantity += 1;
                    GroupingIsAchievedTarget?.Invoke();
                }
            }
        }

        public int IndividualPacking { get; set; }
    }
}