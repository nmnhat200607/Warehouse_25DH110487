using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Warehouse_25DH110487.Models;

namespace Warehouse_25DH110487.Controllers
{
    public class ProductsController : Controller
    {
        private Warehouse_25DH110487Entities db = new Warehouse_25DH110487Entities();

        

        // GET: Products/Create (Câu 2b)
        // GET: Products (Câu 1, 2a & Câu 3: Tìm kiếm + Lọc đồng thời bằng LINQ)
        public ActionResult Index(string searchName, string categoryFilter)
        {
            // Lấy danh sách danh mục không trùng lặp từ CSDL để đổ vào Dropdown
            var categories = db.Products
                               .Select(p => p.Category)
                               .Distinct()
                               .ToList();
            ViewBag.Categories = new SelectList(categories, categoryFilter);
            ViewBag.CurrentSearch = searchName;

            // Khởi tạo truy vấn LINQ (IQueryable)
            var query = db.Products.AsQueryable();

            // 1. Tìm kiếm theo tên sản phẩm (nếu có nhập)
            if (!string.IsNullOrWhiteSpace(searchName))
            {
                query = query.Where(p => p.ProductName.Contains(searchName));
            }

            // 2. Lọc theo danh mục (nếu có chọn)
            if (!string.IsNullOrWhiteSpace(categoryFilter))
            {
                query = query.Where(p => p.Category == categoryFilter);
            }

            var products = query.ToList();
            return View(products);
        }

        // Action phụ để liên kết menu bên trái trỏ thẳng tới trang tìm kiếm/danh sách
        public ActionResult Search()
        {
            return RedirectToAction("Index");
        }

        // POST: Products/Create (Câu 2b)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Product product)
        {
            // Kiểm tra điều kiện tên sản phẩm và danh mục không được để trống
            if (string.IsNullOrWhiteSpace(product.ProductName))
            {
                ModelState.AddModelError("ProductName", "Tên sản phẩm không được để trống.");
            }
            if (string.IsNullOrWhiteSpace(product.Category))
            {
                ModelState.AddModelError("Category", "Danh mục không được để trống.");
            }

            // Kiểm tra đơn giá phải lớn hơn 0
            if (product.Price <= 0)
            {
                ModelState.AddModelError("Price", "Đơn giá phải lớn hơn 0.");
            }

            // Kiểm tra số lượng ban đầu phải lớn hơn hoặc bằng 0
            if (product.Quantity < 0)
            {
                ModelState.AddModelError("Quantity", "Số lượng ban đầu phải lớn hơn hoặc bằng 0.");
            }

            if (ModelState.IsValid)
            {
                db.Products.Add(product);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(product);
        }
        

        // Hàm dùng chung để nạp danh sách sản phẩm kèm số lượng tồn cho Dropdown
        private void LoadProductsDropdown(int? selectedId = null)
        {
            var products = db.Products.ToList()
                .Select(p => new
                {
                    ProductID = p.ProductID,
                    DisplayText = $"{p.ProductName} (Tồn hiện tại: {p.Quantity})"
                });
            ViewBag.ProductID = new SelectList(products, "ProductID", "DisplayText", selectedId);
        }

        // GET: Products/ImportExport (Câu 4a & 4b: Nhập / Xuất kho)
        public ActionResult ImportExport()
        {
            LoadProductsDropdown();
            return View();
        }

        // POST: Products/Import (Câu 4a: Nhập kho)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Import(int? productId, int? importQuantity)
        {
            if (productId == null)
            {
                ModelState.AddModelError("ImportProductID", "Vui lòng chọn sản phẩm cần nhập kho.");
            }

            if (importQuantity == null || importQuantity <= 0)
            {
                ModelState.AddModelError("ImportQuantity", "Số lượng nhập phải là số nguyên dương (lớn hơn 0).");
            }

            if (ModelState.IsValid)
            {
                var product = db.Products.Find(productId);
                if (product != null)
                {
                    product.Quantity = product.Quantity + importQuantity.Value;
                    db.SaveChanges();

                    TempData["SuccessMessage"] = $"Nhập kho thành công +{importQuantity.Value} sản phẩm '{product.ProductName}'. Tồn kho mới: {product.Quantity}.";
                    return RedirectToAction("ImportExport");
                }
            }

            LoadProductsDropdown(productId);
            return View("ImportExport");
        }

        // POST: Products/Export (Câu 4b: Xuất kho)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Export(int? productId, int? exportQuantity)
        {
            if (productId == null)
            {
                ModelState.AddModelError("ExportProductID", "Vui lòng chọn sản phẩm cần xuất kho.");
            }

            if (exportQuantity == null || exportQuantity <= 0)
            {
                ModelState.AddModelError("ExportQuantity", "Số lượng xuất phải là số nguyên dương (lớn hơn 0).");
            }
            else if (productId != null)
            {
                var checkProduct = db.Products.Find(productId);
                if (checkProduct != null && exportQuantity.Value > checkProduct.Quantity)
                {
                    ModelState.AddModelError("ExportQuantity", $"Không thể xuất vượt quá số lượng tồn kho hiện tại ({checkProduct.Quantity}).");
                }
            }

            if (ModelState.IsValid)
            {
                var product = db.Products.Find(productId);
                if (product != null)
                {
                    // Trừ số lượng xuất khỏi số lượng hiện tại
                    product.Quantity = product.Quantity - exportQuantity.Value;
                    db.SaveChanges();

                    // Xác định trạng thái tồn kho mới
                    string trangThai = product.Quantity == 0 ? "Hết hàng" :
                                       (product.Quantity <= 4 ? "Sắp hết hàng" : "Còn hàng");

                    TempData["SuccessMessage"] = $"Xuất kho thành công -{exportQuantity.Value} sản phẩm '{product.ProductName}'. Tồn kho mới: {product.Quantity} (Trạng thái: {trangThai}).";
                    return RedirectToAction("ImportExport");
                }
            }

            LoadProductsDropdown(productId);
            return View("ImportExport");
        }
        // GET: Products/MultiExport (Câu 5: Xuất nhiều sản phẩm trong 1 lần)
        public ActionResult MultiExport()
        {
            var products = db.Products.ToList()
                .Select(p => new
                {
                    ProductID = p.ProductID,
                    DisplayText = $"{p.ProductName} (Tồn hiện tại: {p.Quantity})"
                }).ToList();

            ViewBag.Product1List = new SelectList(products, "ProductID", "DisplayText");
            ViewBag.Product2List = new SelectList(products, "ProductID", "DisplayText");
            return View();
        }

        // POST: Products/MultiExport (Câu 5: Xử lý Transaction)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult MultiExport(int? productId1, int? qty1, int? productId2, int? qty2)
        {
            // 1. Kiểm tra đã chọn đủ 2 sản phẩm chưa
            if (productId1 == null)
            {
                ModelState.AddModelError("Product1Error", "Vui lòng chọn sản phẩm thứ 1.");
            }
            if (productId2 == null)
            {
                ModelState.AddModelError("Product2Error", "Vui lòng chọn sản phẩm thứ 2.");
            }

            // 2. Kiểm tra 2 sản phẩm phải khác nhau
            if (productId1 != null && productId2 != null && productId1 == productId2)
            {
                ModelState.AddModelError("Product2Error", "Hai sản phẩm được chọn phải khác nhau.");
            }

            // 3. Kiểm tra số lượng xuất nguyên dương
            if (qty1 == null || qty1 <= 0)
            {
                ModelState.AddModelError("Qty1Error", "Số lượng xuất sản phẩm 1 phải là số nguyên dương (> 0).");
            }
            if (qty2 == null || qty2 <= 0)
            {
                ModelState.AddModelError("Qty2Error", "Số lượng xuất sản phẩm 2 phải là số nguyên dương (> 0).");
            }

            // 4. Thực hiện kiểm tra tồn kho và cập nhật trong Transaction
            if (ModelState.IsValid)
            {
                using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        var sp1 = db.Products.Find(productId1);
                        var sp2 = db.Products.Find(productId2);

                        if (sp1 == null || sp2 == null)
                        {
                            ModelState.AddModelError("", "Không tìm thấy dữ liệu sản phẩm.");
                        }
                        else if (qty1.Value > sp1.Quantity || qty2.Value > sp2.Quantity)
                        {
                            // Một hoặc cả hai không đủ hàng -> Từ chối toàn bộ, không đổi CSDL
                            if (qty1.Value > sp1.Quantity)
                            {
                                ModelState.AddModelError("Qty1Error", $"Sản phẩm '{sp1.ProductName}' không đủ hàng (Tồn: {sp1.Quantity}, Yêu cầu: {qty1.Value}).");
                            }
                            if (qty2.Value > sp2.Quantity)
                            {
                                ModelState.AddModelError("Qty2Error", $"Sản phẩm '{sp2.ProductName}' không đủ hàng (Tồn: {sp2.Quantity}, Yêu cầu: {qty2.Value}).");
                            }

                            ViewBag.RejectMessage = "TỪ CHỐI TOÀN BỘ YÊU CẦU: Số lượng tồn kho của cả hai sản phẩm vẫn được giữ nguyên.";
                            transaction.Rollback();
                        }
                        else
                        {
                            // Cả hai đều đủ hàng -> Trừ tồn kho và Commit giao dịch
                            sp1.Quantity -= qty1.Value;
                            sp2.Quantity -= qty2.Value;

                            db.SaveChanges();
                            transaction.Commit();

                            TempData["SuccessMessage"] = $"Xuất kho thành công! '{sp1.ProductName}' giảm {qty1.Value} (còn {sp1.Quantity}) và '{sp2.ProductName}' giảm {qty2.Value} (còn {sp2.Quantity}).";
                            return RedirectToAction("MultiExport");
                        }
                    }
                    catch
                    {
                        transaction.Rollback();
                        ModelState.AddModelError("", "Đã xảy ra lỗi hệ thống. Giao dịch đã được hoàn tác.");
                    }
                }
            }

            // Nạp lại dropdown nếu giao dịch bị từ chối hoặc có lỗi nhập liệu
            var products = db.Products.ToList()
                .Select(p => new
                {
                    ProductID = p.ProductID,
                    DisplayText = $"{p.ProductName} (Tồn hiện tại: {p.Quantity})"
                }).ToList();

            ViewBag.Product1List = new SelectList(products, "ProductID", "DisplayText", productId1);
            ViewBag.Product2List = new SelectList(products, "ProductID", "DisplayText", productId2);
            return View();
        }
        // GET: Products/EditList (Trang chọn sản phẩm cần chỉnh sửa từ menu bên trái)
        public ActionResult EditList()
        {
            var products = db.Products.ToList();
            return View(products);
        }

        // GET: Products/Edit/5 (Câu 6: Hiển thị thông tin sản phẩm cần sửa)
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                ViewBag.ErrorMessage = "Thiếu mã sản phẩm (ProductID) cần chỉnh sửa.";
                return View("NotFoundProduct");
            }

            var product = db.Products.Find(id);
            if (product == null)
            {
                ViewBag.ErrorMessage = $"Không tìm thấy sản phẩm có mã ProductID = {id} trong cơ sở dữ liệu.";
                return View("NotFoundProduct");
            }

            return View(product);
        }

        // POST: Products/Edit/5 (Câu 6: Cập nhật ProductName, Category, Price - Giữ nguyên Quantity)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Product product)
        {
            // Kiểm tra điều kiện hợp lệ
            if (string.IsNullOrWhiteSpace(product.ProductName))
            {
                ModelState.AddModelError("ProductName", "Tên sản phẩm không được để trống.");
            }
            if (string.IsNullOrWhiteSpace(product.Category))
            {
                ModelState.AddModelError("Category", "Danh mục không được để trống.");
            }
            if (product.Price <= 0)
            {
                ModelState.AddModelError("Price", "Đơn giá phải lớn hơn 0.");
            }

            // Truy vấn bản ghi gốc từ CSDL
            var existingProduct = db.Products.Find(product.ProductID);
            if (existingProduct == null)
            {
                ViewBag.ErrorMessage = $"Không tìm thấy sản phẩm có mã ProductID = {product.ProductID} để cập nhật.";
                return View("NotFoundProduct");
            }

            if (ModelState.IsValid)
            {
                // Chỉ cập nhật ProductName, Category, Price — TUYỆT ĐỐI KHÔNG đụng tới Quantity
                existingProduct.ProductName = product.ProductName;
                existingProduct.Category = product.Category;
                existingProduct.Price = product.Price;

                db.SaveChanges();
                TempData["SuccessMessage"] = $"Cập nhật thành công thông tin sản phẩm '{existingProduct.ProductName}' (Số lượng tồn kho vẫn giữ nguyên: {existingProduct.Quantity}).";
                return RedirectToAction("EditList");
            }

            // Giữ lại giá trị Quantity hiện tại để hiển thị nếu nhập lỗi
            product.Quantity = existingProduct.Quantity;
            return View(product);
        }
        // GET: Products/Statistics (Câu 7: Thống kê kho hàng bằng LINQ & EF)
        public ActionResult Statistics()
        {
            // 1. Tổng số sản phẩm (số dòng trong bảng Products)
            int totalProducts = db.Products.Count();

            // 2. Số sản phẩm hết hàng (Quantity = 0)
            int outOfStockProducts = db.Products.Count(p => p.Quantity == 0);

            // 3. Tổng giá trị tồn kho = tổng của (Price * Quantity) trên tất cả sản phẩm
            decimal totalInventoryValue = db.Products
                .Select(p => (decimal?)p.Price * p.Quantity)
                .Sum() ?? 0m;

            ViewBag.TotalProducts = totalProducts;
            ViewBag.OutOfStockProducts = outOfStockProducts;
            ViewBag.TotalInventoryValue = totalInventoryValue;

            // Truyền thêm danh sách sản phẩm kèm thành tiền từng dòng để giảng viên dễ đối chiếu số liệu
            var productList = db.Products.ToList();
            return View(productList);
        }
    }
}