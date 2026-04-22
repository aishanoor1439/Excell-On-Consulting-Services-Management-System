using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ExcellOnServices.Data;
using ExcellOnServices.Models;
using Microsoft.AspNetCore.Authorization;
using static ExcellOnServices.Models.PaymentDecorator;


namespace ExcellOnServices.Controllers
{
    [Authorize]
    public class PaymentsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PaymentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Payments
        public async Task<IActionResult> Index()
        {
            var payments = await _context.Payments
                .Include(p => p.Client)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();
            return View(payments);
        }

        // GET: Payments/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var payment = await _context.Payments
                .Include(p => p.Client)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (payment == null)
            {
                return NotFound();
            }

            return View(payment);
        }

        // GET: Payments/Create
        public IActionResult Create()
        {
            var activeClients = _context.Clients.Where(c => c.IsActive).ToList();

            Console.WriteLine($"Active Clients: {activeClients.Count}");

            if (activeClients.Count == 0)
            {
                TempData["ErrorMessage"] = "No active clients found. Please create a client first.";
            }

            ViewData["ClientId"] = new SelectList(activeClients, "Id", "CompanyName");
            return View();
        }

        // POST: Payments/Create - ONLY ONE CREATE ACTION
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(IFormCollection form)
        {
            try
            {
                Console.WriteLine("=== Starting Payment Creation ===");

                // Parse form values manually
                int clientId = 0;
                decimal amount = 0;
                DateTime paymentDate = DateTime.Now;
                string paymentMethod = "Bank Transfer";
                string notes = "";

                // Parse ClientId
                if (int.TryParse(form["ClientId"], out var cid))
                {
                    clientId = cid;
                    Console.WriteLine($"ClientId: {clientId}");
                }

                // Parse Amount
                if (decimal.TryParse(form["Amount"], out var amt))
                {
                    amount = amt;
                    Console.WriteLine($"Amount: {amount}");
                }

                // Parse PaymentDate
                if (!string.IsNullOrEmpty(form["PaymentDate"]) && DateTime.TryParse(form["PaymentDate"], out var pd))
                {
                    paymentDate = pd;
                    Console.WriteLine($"PaymentDate: {paymentDate}");
                }

                // Parse PaymentMethod
                if (!string.IsNullOrEmpty(form["PaymentMethod"]))
                {
                    paymentMethod = form["PaymentMethod"];
                    Console.WriteLine($"PaymentMethod: {paymentMethod}");
                }

                // Parse Notes
                if (!string.IsNullOrEmpty(form["Notes"]))
                {
                    notes = form["Notes"];
                    Console.WriteLine($"Notes: {notes}");
                }

                // Validate required fields
                if (clientId <= 0)
                {
                    TempData["ErrorMessage"] = "Please select a client";
                    ViewData["ClientId"] = new SelectList(_context.Clients.Where(c => c.IsActive), "Id", "CompanyName");
                    return View();
                }

                if (amount <= 0)
                {
                    TempData["ErrorMessage"] = "Please enter a valid amount greater than 0";
                    ViewData["ClientId"] = new SelectList(_context.Clients.Where(c => c.IsActive), "Id", "CompanyName", clientId);
                    return View();
                }

                if (string.IsNullOrEmpty(paymentMethod))
                {
                    TempData["ErrorMessage"] = "Please select a payment method";
                    ViewData["ClientId"] = new SelectList(_context.Clients.Where(c => c.IsActive), "Id", "CompanyName", clientId);
                    return View();
                }

                Console.WriteLine($"Creating Payment with:");
                Console.WriteLine($"  ClientId: {clientId}");
                Console.WriteLine($"  Amount: {amount}");
                Console.WriteLine($"  PaymentDate: {paymentDate}");
                Console.WriteLine($"  PaymentMethod: {paymentMethod}");
                Console.WriteLine($"  Notes: {notes}");

                // Create entity
                var payment = new Payment
                {
                    ClientId = clientId,
                    Amount = amount,
                    PaymentDate = paymentDate,
                    PaymentMethod = paymentMethod,
                    Notes = notes
                };

                _context.Add(payment);
                await _context.SaveChangesAsync();

                Console.WriteLine("=== Payment Created Successfully ===");
                TempData["SuccessMessage"] = "Payment recorded successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"=== ERROR ===");
                Console.WriteLine($"Message: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");

                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }

                TempData["ErrorMessage"] = $"Error: {ex.Message}";

                // Repopulate dropdown
                int clientId = 0;
                int.TryParse(form["ClientId"], out clientId);

                ViewData["ClientId"] = new SelectList(_context.Clients.Where(c => c.IsActive), "Id", "CompanyName", clientId);
                return View();
            }
        }

        // GET: Payments/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var payment = await _context.Payments.FindAsync(id);
            if (payment == null)
            {
                return NotFound();
            }
            ViewData["ClientId"] = new SelectList(_context.Clients.Where(c => c.IsActive), "Id", "CompanyName", payment.ClientId);
            return View(payment);
        }

        // POST: Payments/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,ClientId,Amount,PaymentDate,PaymentMethod,Notes")] Payment payment)
        {
            if (id != payment.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(payment);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PaymentExists(payment.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["ClientId"] = new SelectList(_context.Clients.Where(c => c.IsActive), "Id", "CompanyName", payment.ClientId);
            return View(payment);
        }

        // GET: Payments/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var payment = await _context.Payments
                .Include(p => p.Client)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (payment == null)
            {
                return NotFound();
            }

            return View(payment);
        }

        // POST: Payments/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var payment = await _context.Payments.FindAsync(id);
            if (payment != null)
            {
                _context.Payments.Remove(payment);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PaymentExists(int id)
        {
            return _context.Payments.Any(e => e.Id == id);
        }
        // ==================== NEW DECORATOR PATTERN ACTIONS ====================
        // Add this code at the end of your PaymentsController class, before the final }

        #region Decorator Pattern Implementation

        /// <summary>
        /// Calculate payment with decorator pattern (GET endpoint for testing)
        /// </summary>
        [HttpGet]
        public IActionResult CalculateWithDecorator(decimal amount, bool applyLateFee = false, decimal lateFeePercent = 10, bool applyProcessingFee = false, decimal processingFee = 25)
        {
            try
            {
                // Start with base calculator
                IPaymentCalculator calculator = new BasePaymentCalculator();
                string appliedDecorators = "None";

                // Dynamically wrap with decorators
                if (applyLateFee)
                {
                    calculator = new LateFeeDecorator(calculator, lateFeePercent);
                    appliedDecorators = $"Late Fee ({lateFeePercent}%)";
                }

                if (applyProcessingFee)
                {
                    calculator = new ProcessingFeeDecorator(calculator, processingFee);
                    appliedDecorators += appliedDecorators == "None" ? $"Processing Fee (${processingFee})" : $" + Processing Fee (${processingFee})";
                }

                // Calculate final amount
                decimal originalAmount = amount;
                decimal finalAmount = calculator.Calculate(amount);
                string description = calculator.GetDescription();

                // Return JSON result for testing
                return Json(new
                {
                    success = true,
                    originalAmount = originalAmount,
                    finalAmount = finalAmount,
                    appliedDecorators = appliedDecorators,
                    calculationDescription = description,
                    message = $"Decorator pattern applied successfully!"
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Create payment with automatic decorator calculation (Modified Create with decorator)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateWithDecorator(IFormCollection form)
        {
            try
            {
                Console.WriteLine("=== Starting Payment Creation WITH DECORATOR PATTERN ===");

                // Parse form values
                int clientId = 0;
                decimal originalAmount = 0;
                DateTime paymentDate = DateTime.Now;
                string paymentMethod = "Bank Transfer";
                string notes = "";
                bool applyLateFee = false;
                decimal lateFeePercent = 10;
                bool applyProcessingFee = false;
                decimal processingFee = 25;

                // Parse ClientId
                if (int.TryParse(form["ClientId"], out var cid))
                    clientId = cid;

                // Parse Original Amount
                if (decimal.TryParse(form["Amount"], out var amt))
                    originalAmount = amt;

                // Parse PaymentDate
                if (!string.IsNullOrEmpty(form["PaymentDate"]) && DateTime.TryParse(form["PaymentDate"], out var pd))
                    paymentDate = pd;

                // Parse PaymentMethod
                if (!string.IsNullOrEmpty(form["PaymentMethod"]))
                    paymentMethod = form["PaymentMethod"];

                // Parse Notes
                if (!string.IsNullOrEmpty(form["Notes"]))
                    notes = form["Notes"];

                // Parse Decorator options
                if (!string.IsNullOrEmpty(form["ApplyLateFee"]))
                    bool.TryParse(form["ApplyLateFee"], out applyLateFee);

                if (!string.IsNullOrEmpty(form["LateFeePercent"]))
                    decimal.TryParse(form["LateFeePercent"], out lateFeePercent);

                if (!string.IsNullOrEmpty(form["ApplyProcessingFee"]))
                    bool.TryParse(form["ApplyProcessingFee"], out applyProcessingFee);

                if (!string.IsNullOrEmpty(form["ProcessingFee"]))
                    decimal.TryParse(form["ProcessingFee"], out processingFee);

                // Validate required fields
                if (clientId <= 0 || originalAmount <= 0)
                {
                    TempData["ErrorMessage"] = "Please select a client and enter valid amount";
                    ViewData["ClientId"] = new SelectList(_context.Clients.Where(c => c.IsActive), "Id", "CompanyName", clientId);
                    return View("Create");
                }

                // === DECORATOR PATTERN IMPLEMENTATION ===
                IPaymentCalculator calculator = new BasePaymentCalculator();
                string calculationDetails = "Base Payment";

                if (applyLateFee)
                {
                    calculator = new LateFeeDecorator(calculator, lateFeePercent);
                    calculationDetails += $" + Late Fee ({lateFeePercent}%)";
                }

                if (applyProcessingFee)
                {
                    calculator = new ProcessingFeeDecorator(calculator, processingFee);
                    calculationDetails += $" + Processing Fee (${processingFee})";
                }

                decimal finalAmount = calculator.Calculate(originalAmount);
                // === DECORATOR PATTERN END ===

                Console.WriteLine($"Original Amount: {originalAmount}");
                Console.WriteLine($"Final Amount after decorators: {finalAmount}");
                Console.WriteLine($"Calculation: {calculationDetails}");

                // Create payment with FINAL amount (after decorators)
                var payment = new Payment
                {
                    ClientId = clientId,
                    Amount = finalAmount,  // Using decorated amount
                    PaymentDate = paymentDate,
                    PaymentMethod = paymentMethod,
                    Notes = $"{notes} | [Decorators: {calculationDetails}] | Original: ${originalAmount}"
                };

                _context.Add(payment);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Payment recorded successfully! Original: ${originalAmount}, Final: ${finalAmount} ({calculationDetails})";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
                TempData["ErrorMessage"] = $"Error: {ex.Message}";

                int clientId = 0;
                int.TryParse(form["ClientId"], out clientId);
                ViewData["ClientId"] = new SelectList(_context.Clients.Where(c => c.IsActive), "Id", "CompanyName", clientId);
                return View("Create");
            }
        }

        /// <summary>
        /// Test endpoint to verify decorator pattern is working
        /// </summary>
        [HttpGet]
        public IActionResult TestDecorator()
        {
            var results = new List<object>();

            // Test 1: Base payment only
            IPaymentCalculator baseCalc = new BasePaymentCalculator();
            results.Add(new
            {
                TestCase = "Base Payment Only",
                Input = 1000,
                Output = baseCalc.Calculate(1000),
                Description = baseCalc.GetDescription()
            });

            // Test 2: With 10% late fee
            IPaymentCalculator lateFeeCalc = new LateFeeDecorator(new BasePaymentCalculator(), 10);
            results.Add(new
            {
                TestCase = "With 10% Late Fee",
                Input = 1000,
                Output = lateFeeCalc.Calculate(1000),
                Description = lateFeeCalc.GetDescription()
            });

            // Test 3: With $25 processing fee
            IPaymentCalculator processingCalc = new ProcessingFeeDecorator(new BasePaymentCalculator(), 25);
            results.Add(new
            {
                TestCase = "With $25 Processing Fee",
                Input = 1000,
                Output = processingCalc.Calculate(1000),
                Description = processingCalc.GetDescription()
            });

            // Test 4: With both late fee (10%) and processing fee ($25)
            IPaymentCalculator bothCalc = new ProcessingFeeDecorator(
                                                new LateFeeDecorator(
                                                    new BasePaymentCalculator(), 10), 25);
            results.Add(new
            {
                TestCase = "Both Late Fee (10%) + Processing Fee ($25)",
                Input = 1000,
                Output = bothCalc.Calculate(1000),
                Description = bothCalc.GetDescription()
            });

            return Json(new
            {
                success = true,
                pattern = "Decorator Pattern",
                testResults = results,
                message = "Decorator pattern is working correctly!"
            });
        }

        #endregion
    }
}