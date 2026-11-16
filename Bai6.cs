/***************************************************
 * Mã sinh viên: 202418909
 * Họ tên: Trần Quốc Hùng
 ***************************************************/

using System;
using System.Collections.Generic;
using System.Linq;

namespace DeviceManagement
{
    // 1. Enum Trạng thái
    public enum DeviceStatus { Available, Borrowed, UnderMaintenance, Retired }

    // 2. Ngoại lệ dùng chung 
    public class BorrowingException : Exception
    {
        public BorrowingException(string message) : base(message) { }
    }

    // 3. Lớp trừu tượng Thiết bị & Các lớp con
    public abstract class Device
    {
        public string DeviceId { get; }
        public string Name { get; set; }
        public int ImportYear { get; set; }
        public decimal PurchasePrice { get; set; }
        public DeviceStatus Status { get; set; } = DeviceStatus.Available;

        public Device(string id, string name, int year, decimal price)
        {
            if (string.IsNullOrEmpty(id) || price <= 0 || year > DateTime.Now.Year)
                throw new BorrowingException("Thông tin thiết bị không hợp lệ!");
            DeviceId = id; Name = name; ImportYear = year; PurchasePrice = price;
        }

        public abstract int MaxBorrowDays { get; }
        public abstract decimal CalculateAnnualMaintenanceCost();
    }

    public class AirConditionerRemote : Device
    {
        public AirConditionerRemote(string id, string name, int year, decimal price) : base(id, name, year, price) { }
        public override int MaxBorrowDays => 7;
        public override decimal CalculateAnnualMaintenanceCost() => PurchasePrice * 0.05m;
    }

    public class Microphone : Device
    {
        public bool IsWireless { get; set; }
        public Microphone(string id, string name, int year, decimal price, bool isWireless) : base(id, name, year, price) => IsWireless = isWireless;
        public override int MaxBorrowDays => 1;
        public override decimal CalculateAnnualMaintenanceCost() => PurchasePrice * (IsWireless ? 0.12m : 0.08m);
    }

    public class ProjectorRemote : Device
    {
        public ProjectorRemote(string id, string name, int year, decimal price) : base(id, name, year, price) { }
        public override int MaxBorrowDays => 3;
        public override decimal CalculateAnnualMaintenanceCost() => PurchasePrice * 0.06m;
    }

    // 4. Interface Người mượn & 2 lớp thực thi
    public interface IBorrower
    {
        string BorrowerId { get; }
        string FullName { get; }
        int GetMaximumDevices();
    }

    public class Lecturer : IBorrower
    {
        public string BorrowerId { get; }
        public string FullName { get; }
        public Lecturer(string id, string name) { BorrowerId = id; FullName = name; }
        public int GetMaximumDevices() => 3;
    }

    public class TeachingAssistant : IBorrower
    {
        public string BorrowerId { get; }
        public string FullName { get; }
        public TeachingAssistant(string id, string name) { BorrowerId = id; FullName = name; }
        public int GetMaximumDevices() => 2;
    }

    // 5. Chi tiết mượn & Phiếu mượn
    public class BorrowItem
    {
        public Device Device { get; }
        public DateTime BorrowedDate { get; }
        public DateTime DueDate { get; }
        public DateTime? ReturnedDate { get; private set; }
        public bool IsReturned => ReturnedDate.HasValue;
        public bool IsLate => ReturnedDate.HasValue && ReturnedDate.Value > DueDate;

        public BorrowItem(Device device, DateTime borrowedDate, int borrowDays)
        {
            Device = device;
            BorrowedDate = borrowedDate;
            DueDate = borrowedDate.AddDays(borrowDays);
        }

        public void Return(DateTime returnedDate)
        {
            if (IsReturned) throw new BorrowingException($"Thiết bị {Device.DeviceId} đã được trả trước đó!");
            if (returnedDate < BorrowedDate) throw new BorrowingException("Ngày trả không được trước ngày mượn!");
            ReturnedDate = returnedDate;
            Device.Status = DeviceStatus.Available;
        }
    }

    public class BorrowingSlip
    {
        public string SlipId { get; }
        public IBorrower Borrower { get; }
        public string TeachingRoom { get; }
        public DateTime BorrowedDate { get; }
        public List<BorrowItem> Items { get; } = new List<BorrowItem>();

        public BorrowingSlip(string slipId, IBorrower borrower, string room, DateTime date)
        {
            SlipId = slipId; Borrower = borrower; TeachingRoom = room; BorrowedDate = date;
        }

        public void AddDevice(Device device, int borrowDays)
        {
            if (device == null) throw new BorrowingException("Thiết bị không tồn tại!");
            if (borrowDays <= 0 || borrowDays > device.MaxBorrowDays) 
                throw new BorrowingException($"Mượn {borrowDays} ngày vượt quá giới hạn ({device.MaxBorrowDays} ngày) của thiết bị {device.DeviceId}!");
            if (Items.Any(i => i.Device.DeviceId == device.DeviceId)) 
                throw new BorrowingException($"Thiết bị {device.DeviceId} đã có trong phiếu!");
            if (Items.Count >= Borrower.GetMaximumDevices()) 
                throw new BorrowingException($"Người mượn {Borrower.FullName} đã đạt giới hạn tối đa ({Borrower.GetMaximumDevices()} thiết bị)!");
            if (device.Status != DeviceStatus.Available) 
                throw new BorrowingException($"Thiết bị {device.DeviceId} ({device.Name}) đang ở trạng thái {device.Status}, không thể mượn!");

            device.Status = DeviceStatus.Borrowed;
            Items.Add(new BorrowItem(device, BorrowedDate, borrowDays));
        }

        public void ReturnDevice(string deviceId, DateTime returnDate)
        {
            var item = Items.FirstOrDefault(i => i.Device.DeviceId == deviceId);
            if (item == null) throw new BorrowingException($"Thiết bị {deviceId} không thuộc phiếu {SlipId}!");
            item.Return(returnDate);
        }
    }

    // 6. Kiểm thử
    class Program
    {
        static void Main()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.WriteLine("LUỒNG KIỂM THỬ THÀNH CÔNG\n");

            // 1. Tạo một giảng viên.
            var gv = new Lecturer("GV01", "TS. Nguyễn Văn A");
            Console.WriteLine($"1. Tạo giảng viên: {gv.FullName} (Số thiết bị mượn tối đa: {gv.GetMaximumDevices()})");

            // 2. Tạo một điều khiển máy chiếu, một điều khiển điều hòa và một micro không dây.
            var proj = new ProjectorRemote("PR01", "Điều khiển Máy chiếu", 2022, 500000);
            var ac = new AirConditionerRemote("AC01", "Điều khiển Điều hòa", 2023, 350000);
            var mic = new Microphone("MC01", "Micro Không dây", 2023, 1000000, isWireless: true);
            Console.WriteLine("2. Đã tạo 3 thiết bị: Máy chiếu (PR01), Điều hòa (AC01), Micro (MC01)");

            // 3. Tạo phiếu mượn cho một phòng dạy.
            DateTime today = DateTime.Now.Date;
            var slip1 = new BorrowingSlip("SLIP01", gv, "D9-401", today);
            Console.WriteLine($"3. Tạo phiếu mượn {slip1.SlipId} cho phòng {slip1.TeachingRoom}");

            // 4. Thêm điều khiển máy chiếu và micro vào phiếu.
            slip1.AddDevice(proj, 3); // Mượn máy chiếu 3 ngày
            slip1.AddDevice(mic, 1);  // Mượn micro 1 ngày
            Console.WriteLine("4. Đã thêm máy chiếu (PR01) và micro (MC01) vào phiếu.");

            // 5. Trả điều khiển máy chiếu đúng hạn.
            slip1.ReturnDevice("PR01", today.AddDays(3));
            Console.WriteLine("5. Trả điều khiển máy chiếu (PR01) đúng hạn thành công.");

            // 6. Trả micro muộn 2 ngày.
            slip1.ReturnDevice("MC01", today.AddDays(3));
            Console.WriteLine("6. Trả micro (MC01) muộn 2 ngày thành công.");

            // 7. Hiển thị các phiếu trả muộn, chưa trả.
            Console.WriteLine("\n7. HIỂN THỊ THỐNG KÊ TRẠNG THÁI TRẢ THIẾT BỊ:");
            foreach (var item in slip1.Items)
            {
                string status = "CHƯA TRẢ";
                if (item.IsReturned)
                {
                    if (item.IsLate)
                    {
                        int lateDays = (item.ReturnedDate.Value - item.DueDate).Days;
                        status = $"ĐÃ TRẢ (MUỘN {lateDays} NGÀY)";
                    }
                    else
                    {
                        status = "ĐÃ TRẢ (ĐÚNG HẠN)";
                    }
                }
                Console.WriteLine($" - [{item.Device.DeviceId}] {item.Device.Name} | Hạn trả: {item.DueDate:dd/MM/yyyy} | Trạng thái: {status}");
            }

            Console.WriteLine("LUỒNG PHÁT SINH NGOẠI LỆ\n");

            // 1. Mượn điều khiển điều hòa đang UnderMaintenance.
            var acBroken = new AirConditionerRemote("AC02", "Điều khiển Điều hòa Hỏng", 2021, 300000) 
            { 
                Status = DeviceStatus.UnderMaintenance 
            };
            var slipTest = new BorrowingSlip("SLIP_TEST", gv, "D3-201", today);
            TestScenario("1. Mượn điều khiển điều hòa đang UnderMaintenance", 
                () => slipTest.AddDevice(acBroken, 2));

            // 2. Trợ giảng mượn thiết bị thứ ba.
            var ta = new TeachingAssistant("TA01", "Trợ giảng Lê Văn B");
            var slipTA = new BorrowingSlip("SLIP_TA", ta, "D3-202", today);
            var dev1 = new ProjectorRemote("PR02", "Máy chiếu 2", 2022, 400000);
            var dev2 = new AirConditionerRemote("AC03", "Điều hòa 3", 2022, 400000);
            var dev3 = new Microphone("MC02", "Micro 2", 2022, 400000, false);
            slipTA.AddDevice(dev1, 1);
            slipTA.AddDevice(dev2, 1);
            TestScenario("2. Trợ giảng mượn thiết bị thứ 3 (vượt quá giới hạn 2 thiết bị)", 
                () => slipTA.AddDevice(dev3, 1));

            // 3. Thêm cùng một micro hai lần.
            var slipDup = new BorrowingSlip("SLIP_DUP", gv, "D3-203", today);
            var micDup = new Microphone("MC03", "Micro Shure", 2023, 800000, true);
            slipDup.AddDevice(micDup, 1);
            TestScenario("3. Thêm cùng một micro 2 lần vào phiếu", 
                () => slipDup.AddDevice(micDup, 1));

            // 4. Mượn micro trong 3 ngày dù giới hạn là 1 ngày.
            var micLimit = new Microphone("MC04", "Micro Sony", 2023, 900000, true);
            var slipLimit = new BorrowingSlip("SLIP_LIMIT", gv, "D3-204", today);
            TestScenario("4. Mượn micro trong 3 ngày dù giới hạn là 1 ngày", 
                () => slipLimit.AddDevice(micLimit, 3));

            // 5. Trả một điều khiển máy chiếu không thuộc phiếu.
            var slipReturnErr = new BorrowingSlip("SLIP_RET", gv, "D3-205", today);
            TestScenario("5. Trả điều khiển máy chiếu không thuộc phiếu", 
                () => slipReturnErr.ReturnDevice("PR99", today));

            // 6. Trả cùng một thiết bị hai lần.
            var slipDoubleRet = new BorrowingSlip("SLIP_DBL", gv, "D3-206", today);
            var projRet = new ProjectorRemote("PR03", "Máy chiếu 3", 2022, 500000);
            slipDoubleRet.AddDevice(projRet, 2);
            slipDoubleRet.ReturnDevice("PR03", today);
            TestScenario("6. Trả cùng một thiết bị 2 lần", 
                () => slipDoubleRet.ReturnDevice("PR03", today));

            // 7. Trả thiết bị với ngày trả trước ngày mượn.
            var slipDateErr = new BorrowingSlip("SLIP_DATE", gv, "D3-207", today);
            var projDate = new ProjectorRemote("PR04", "Máy chiếu 4", 2022, 500000);
            slipDateErr.AddDevice(projDate, 2);
            TestScenario("7. Trả thiết bị với ngày trả trước ngày mượn", 
                () => slipDateErr.ReturnDevice("PR04", today.AddDays(-1)));
        }

        static void TestScenario(string name, Action action)
        {
            Console.Write($"\n[TEST] {name}\n -> KẾT QUẢ: ");
            try
            {
                action();
                Console.WriteLine("THÀNH CÔNG (Không bị lỗi)");
            }
            catch (BorrowingException ex)
            {
                Console.WriteLine($"BẮT NGOẠI LỆ THÀNH CÔNG: {ex.Message}");
            }
        }
    }
}