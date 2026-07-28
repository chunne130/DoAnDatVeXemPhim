const fs = require('fs');
let content = fs.readFileSync('Views/Shared/_Layout.cshtml', 'utf8');
content = content.replace(/Trang Ch.*/g, 'Trang Chủ</a>');
content = content.replace(/Phim Dang Chi.*/g, 'Phim Đang Chiếu</a>');
content = content.replace(/Phim S.*p Chi.*/g, 'Phim Sắp Chiếu</a>');
content = content.replace(/<a class="dR\?p Chi\?u<\/a>/g, '<a class="dropdown-item dropdown-item-custom" asp-controller="Home" asp-action="Upcoming">Phim Sắp Chiếu</a>');
content = content.replace(/R\?p Chi\?u/g, 'Rạp Chiếu');
content = content.replace(/Uu Dai/g, 'Ưu Đãi');
content = content.replace(/T\?m t\?n phim/g, 'Tìm tên phim');

fs.writeFileSync('Views/Shared/_Layout.cshtml', content, 'utf8');
