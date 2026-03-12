using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace WebBanHang_2380600870.Migrations
{
    /// <inheritdoc />
    public partial class SeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "Trà & Thảo Mộc" },
                    { 2, "Cà Phê" },
                    { 3, "Bánh Sáng" }
                });

            migrationBuilder.InsertData(
                table: "Products",
                columns: new[] { "Id", "CategoryId", "Description", "ImageUrl", "Name", "Price" },
                values: new object[,]
                {
                    { 1, 1, "Hoa cúc hữu cơ dịu nhẹ, pha cùng mật ong rừng địa phương và một chút vani.", "https://lh3.googleusercontent.com/aida-public/AB6AXuCEAjGgjkSer5q9ba5D6NyzN1c9lTOJPMXWpNJltT_GLw0NEh93dsoZsJaXB20ZNlnBukK0BvyNWGB6fwjYu-mP17nG4ALpaDwiVOTRHcQvCf1w1Ib5UbFX9y76GFKvPekabC6V_VgoEm-Zc1Y_S96uZErX-tYH5XAeDCbc7TxftVTD-rmBHuOd7aSkx881C8NtH-mc5rMhj4dyuSomr4y-1PPGwQdiN893dwpZjGmWyehq9XIq_Ba8NDWvScXE1Tf9RBlP3LwPkGkJ", "Trà Hoa Cúc Mật Ong", 55000m },
                    { 2, 1, "Cánh atiso đỏ giàu vitamin, kết hợp hoa hồng Bulgaria khô cho hương thơm thanh nhẹ.", "https://lh3.googleusercontent.com/aida-public/AB6AXuBquJBlkgANzMfyJGh1XkspjUt3umtwxQzexDd8I0mt0FgPpx6FXkExK_M7Xm-wM6-LP7BG6Uy4DlU4Zs_SLn4nniVk8-ZjKvOweTbUCwoZ4qlVJ3Ng2_hYfQ3E6HoYycQvSScsSUO1HRJnZF3sizIF_rZ_Dy-byN31UV3fduUnRR-A1zsaH7m7UztM6dHj1VAVWReoWicfHForfNH45bxgCWHUAofQXMqeY9jauQltovivmnmKywndNVfLUxF3nPKs7Lg3ozOwc5Re", "Trà Hoa Atiso Đỏ", 58000m },
                    { 3, 1, "Trà đen thượng hạng ướp tinh dầu bergamot và cánh hoa ngô xanh — cổ điển và tinh tế.", "https://lh3.googleusercontent.com/aida-public/AB6AXuD-HrH2opygixRAPA4_hKWugp4IUUWQdv8LY4AAqkAJ4eUISM5MqQu2ZqCQaD_fj6h5ujbIB7gwlMDgzTEGuN5-L7j_J49jf5Y6my037ZvILcU-yaOt5EGfRdq-mRf5ILDr5o8oYHvOHa4-EBvZbGfClfC1TQjlYMdoHZWYEb7qY9tlI0CbLCoHI4MXnOdGG8P02A8BEg1Y9ZhMI8m8EC_B-7UvsW1IxAKATnSeQzc2Iava4vUQGKDILPm_YIYx-pZDsddI8-ILgzY6", "Earl Grey Hoàng Gia", 55000m },
                    { 4, 1, "Trà xanh cuộn tay, ướp hoa lài tươi nhiều lần — hương thơm dịu mà sâu lắng.", "https://lh3.googleusercontent.com/aida-public/AB6AXuDQqi2HYPgIa_kB6UkOUsv1ob9VljdNcRiPmydMW8pDePwjEl8EvWaBKZd6VTeetyXv31Jee_gx0IAf5xy6B129-6OEQNi9FUFN42I4CkNVtsJrZGES7TJSBdXDHFauYzCKd1YEq1oUKfqEsfs8n-n0sw2wy7TzjnveLWlJLQyBiw-geTWQg_J8kRLA7hpIo5euMXC5oppW3jMTIKImMRauGsxp1-19-wTW6LG1IosZWTlhpYXUMLc58UBBXuKlsIxRHhTJuu-0i5oU", "Trà Lài Ngọc Châu", 65000m },
                    { 5, 2, "Robusta đậm đà, phủ lớp lòng đỏ trứng đánh bông cùng sữa đặc — đặc sản không thể thiếu.", "https://lh3.googleusercontent.com/aida-public/AB6AXuBtk6EC-fylhVI5xH_hiJ8RDZU-69cXHO2s1AehaDaLNDlCJYITX2Zxb1Z9FjqLPonF1rKl5UbilrIOwd10DxLcnVcB1sYqKd-q-cBzkMML0GEQK84wxT31BEbpLz89Raqu3e8S6B6y6eyfYz8NMNZ6uy86k8v6sEjh0KsoEA_9mz8VGql5gajQZ0kafubfK55b95S9AjEflczQl7mYmLhBnNE6M70ea_tSmPbAHHcZTKFHTypRezqxN7Cm--NywSQi8Su4cmDVEECJ", "Cà Phê Trứng Hà Nội", 65000m },
                    { 6, 2, "Blend Arabica & Robusta, chiết xuất chuẩn mực — crema vàng óng, đậm mà không gắt.", "https://lh3.googleusercontent.com/aida-public/AB6AXuBAgg0VukbL8PkMBh630VrgmEo5GlHTpzYcPsve8JYTreMPfDcsUXIpEGgQxW_Mw_xFs2A7aPigCqb8WBVfvYVbkQRIfQPihVRu9nSX8LVqBRs9-Bx-EfvJPEypDdb9Eh2IsKp9QGrGb-Ltj40PMoFo15yqvF2Vgf6mwzVUQ9f52k5I_UIbONcIO9eWA9PwlzcgN8OJR38B-a5VQ-CXMapEAYOQf3-ibyb8jEm4anTRkFjkDMIoZkEEOBTMLQzS0d8SfCcvfAX_0qKO", "Espresso Truyền Thống", 35000m },
                    { 7, 2, "Ủ lạnh suốt 18 tiếng — vị mượt mà, ít đắng, thoảng hương sô-cô-la nhẹ nhàng.", "https://lh3.googleusercontent.com/aida-public/AB6AXuAmJUt1qccm5508I5vD_7QhOCniGdlCnerKYHZODhzkWxHJIA_t1SglSieq6JrPBXjg0YRwJjOIHzaOhaFNsdpo5mt7bABv4IHhr2iFalxHhNNpghXL3EFetndNXXGE0AHca9SvXHiTeroRThJ7BlNPHifK1w01nGqKZ4eFo5mDoYOy6hp-ozZpOBnEC4yJRCbrCkxAmYn2Q8lCulkG1zehU5QCcNhiciqPVQSKmkDozC9BtdGSfRAGLxqbSOO6xXQk4iCT6-5RAb_r", "Cold Brew 18 Giờ", 50000m },
                    { 8, 2, "Sữa tươi đánh bọt mịn như nhung, rót trên hai shot espresso — thêm chút quế cho ấm lòng.", "https://lh3.googleusercontent.com/aida-public/AB6AXuAtK1_y4hfIsSD370bZL11idnq18mh0bc-3O46w-21oCAtSxTFfm17M9PS-ii40W8IhN967PdrnBbmx5P4Ra2fYE4vQCZ99yVEEWSXMCnw4Fltbt_F8pBkmhOEfDtILMliJIzUQ0pOult2Rm8kqS2WR-p7MxwYFeInvMr8vCXChS_YafKfUERTuBmkmpE6fc7zxbuUFXv3u4s3QEXLmUWB1RJtPU2-4tPeNr-CkaBdM21mDlNgrLLo06F_Nd08Bw5PsEuxutMyn6316", "Latte Nhung", 45000m },
                    { 9, 3, "Nướng hai lần, nhân kem hạnh nhân, rắc hạnh nhân lát lên trên — giòn tan và thơm lừng.", "https://lh3.googleusercontent.com/aida-public/AB6AXuDuXIB-ebcP8NsQJpZGJoF555uRr-TrEgQ_s7NJOUSSS9-HqIXFAHZeJtkttWZfQ8sPueSPg_0ws4eiuadUjFUW1N1ErYvkqhQPGgGsdAssgzGXgux5wXpORtpK6Bduqw8IeBZaJIRfArYsKVdSUGAEYh8H4FOgRHP9y3AaqLGZB9n8_0h40-m73xcy2x0UN60LDpOZicMQpeHKmAFax9Nee9jXF_z6gJSvMaXf7cv2qln-xesVIxQazR1xqZrIUgUNI5mg7IqRGeFF", "Croissant Hạnh Nhân", 38000m },
                    { 10, 3, "Mứt trái cây theo mùa trên nền bánh ngàn lớp giòn rụm, phủ kem trứng vani nhẹ nhàng.", "https://lh3.googleusercontent.com/aida-public/AB6AXuCN56R6kgmsS-aVc-eplM5QDI5vQHrjwyeV_szA7HBIFtj7td95uvjPXEvW4uyTU7U5Ef28-H4FAVRwr4A-a-X-Hj_K7UJzCGI_f6_Zw7TEBi23Feimclr93XrVM_PtHX6dkj_UcpWRBbZ8Jd9EFslVSFsxJi_Ltzo-xPWPVgFOt9S1WSvMq1a6alIAMS5-apdbb34SRWlJVU36erUQCTZx05UgWKz1YQKeB1GEhRHjRPUqsK2mn2UWGTfw6JUduGg6WZnmrDJ1xksr", "Bánh Trái Cây Mùa", 35000m },
                    { 11, 3, "Hai lát bánh mì chua thủ công nướng vàng, ăn kèm bơ lên men và mứt dâu rừng tự làm.", "https://lh3.googleusercontent.com/aida-public/AB6AXuCFGhQ6PA3PWH7EXeIIw2LwXxKOZWXr6dCzy4iM9UAn8xYcbuUD1uqhYNyQtgzpaavc1tbDt9e11zg6xyih5kuREdjtIqAbcWio4BqKpJ1n4kFpCf8cutPqkeSukUSnvRlNXOCKP_h0h95C2E0Qp89eRaZRbyGSgjL1OUEgnMjmQ4iEeYILEp4uDy0jmrYhv4S4xRUbiYbJmIvsfvRTbI2JkYBZVYWe1_YZLIiq3xokHn46ysYFenexcGQiICdUHPPph4ZuUiwWzipa", "Bánh Mì Chua & Mứt", 42000m }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 3);
        }
    }
}
