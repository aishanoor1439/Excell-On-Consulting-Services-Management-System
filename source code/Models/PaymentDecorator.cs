namespace ExcellOnServices.Models
{
    public class PaymentDecorator
    {
        // ==================== 1. INTERFACE ====================
        public interface IPaymentCalculator
        {
            decimal Calculate(decimal amount);
            string GetDescription();
        }

        // ==================== 2. BASE CALCULATOR (CORE PAYMENT) ====================
        public class BasePaymentCalculator : IPaymentCalculator
        {
            public decimal Calculate(decimal amount)
            {
                // Base payment - no extra charges
                return amount;
            }

            public string GetDescription()
            {
                return "Base Payment (Full Amount)";
            }
        }

        // ==================== 3. ABSTRACT DECORATOR ====================
        public abstract class PaymentDecoratorBase : IPaymentCalculator
        {
            protected IPaymentCalculator _calculator;

            protected PaymentDecoratorBase(IPaymentCalculator calculator)
            {
                _calculator = calculator;
            }

            public virtual decimal Calculate(decimal amount)
            {
                return _calculator.Calculate(amount);
            }

            public virtual string GetDescription()
            {
                return _calculator.GetDescription();
            }
        }

        // ==================== 4. CONCRETE DECORATOR - LATE FEE ====================
        public class LateFeeDecorator : PaymentDecoratorBase
        {
            private decimal _lateFeePercentage;

            public LateFeeDecorator(IPaymentCalculator calculator, decimal lateFeePercentage)
                : base(calculator)
            {
                _lateFeePercentage = lateFeePercentage;
            }

            public override decimal Calculate(decimal amount)
            {
                decimal lateFee = amount * (_lateFeePercentage / 100);
                decimal totalWithLateFee = base.Calculate(amount) + lateFee;
                return totalWithLateFee;
            }

            public override string GetDescription()
            {
                return $"{_calculator.GetDescription()} + Late Fee ({_lateFeePercentage}%)";
            }
        }

        // ==================== 5. CONCRETE DECORATOR - PROCESSING FEE (NEW) ====================
        public class ProcessingFeeDecorator : PaymentDecoratorBase
        {
            private decimal _fixedFee;

            public ProcessingFeeDecorator(IPaymentCalculator calculator, decimal fixedFee)
                : base(calculator)
            {
                _fixedFee = fixedFee;
            }

            public override decimal Calculate(decimal amount)
            {
                return base.Calculate(amount) + _fixedFee;
            }

            public override string GetDescription()
            {
                return $"{_calculator.GetDescription()} + Processing Fee (${_fixedFee})";
            }
        }

        // ==================== 6. EXTRA: DISCOUNT DECORATOR (OPTIONAL) ====================
        public class DiscountDecorator : PaymentDecoratorBase
        {
            private decimal _discountPercentage;

            public DiscountDecorator(IPaymentCalculator calculator, decimal discountPercentage)
                : base(calculator)
            {
                _discountPercentage = discountPercentage;
            }

            public override decimal Calculate(decimal amount)
            {
                decimal discount = amount * (_discountPercentage / 100);
                decimal totalAfterDiscount = base.Calculate(amount) - discount;
                return totalAfterDiscount < 0 ? 0 : totalAfterDiscount;
            }

            public override string GetDescription()
            {
                return $"{_calculator.GetDescription()} + Discount ({_discountPercentage}%)";
            }
        }
    }
}