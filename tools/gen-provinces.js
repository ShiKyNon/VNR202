// Generates src/HanhTrinh.Web/Data/provinces.json: path order (spec §5.1), tags (§5.2), stylised coords from approx. lat/lon + repulsion.
const fs = require('fs');
const P = [
 ['Hà Giang',,'Bac',22.82,104.98],['Cao Bằng',,'Bac',22.67,106.26],['Lạng Sơn',,'Bac',21.85,106.76],['Quảng Ninh',,'Bac',21.0,107.3],
 ['Bắc Giang',,'Bac',21.27,106.19],['Thái Nguyên',,'Bac',21.59,105.85],['Bắc Kạn',,'Bac',22.15,105.83],['Tuyên Quang',,'Bac',21.82,105.21],
 ['Yên Bái',,'Bac',21.72,104.6],['Lào Cai',,'Bac',22.48,103.97],['Lai Châu',,'Bac',22.4,103.3],['Điện Biên',,'Bac',21.39,103.02],
 ['Sơn La',,'Bac',21.2,103.91],['Hòa Bình',,'Bac',20.75,105.2],['Phú Thọ',,'Bac',21.32,105.3],['Vĩnh Phúc',,'Bac',21.38,105.6],
 ['Hà Nội',,'Bac',21.03,105.85],['Bắc Ninh',,'Bac',21.18,106.07],['Hải Dương',,'Bac',20.94,106.33],['Hải Phòng',,'Bac',20.84,106.69],
 ['Thái Bình',,'Bac',20.45,106.4],['Hưng Yên',,'Bac',20.65,106.06],['Hà Nam',,'Bac',20.54,105.85],['Nam Định',,'Bac',20.3,106.17],
 ['Ninh Bình',,'Bac',20.2,105.9],
 ['Thanh Hóa',,'Trung',19.81,105.6],['Nghệ An',,'Trung',18.9,105.4],['Hà Tĩnh',,'Trung',18.34,105.91],['Quảng Bình',,'Trung',17.47,106.4],
 ['Quảng Trị',,'Trung',16.75,107.0],['Thừa Thiên Huế','Huế','Trung',16.4,107.6],['Đà Nẵng',,'Trung',16.05,108.2],['Hoàng Sa',,'Trung',16.5,111.9],
 ['Quảng Nam',,'Trung',15.57,108.3],['Quảng Ngãi',,'Trung',15.12,108.8],['Bình Định',,'Trung',13.9,109.1],['Phú Yên',,'Trung',13.09,109.3],
 ['Khánh Hòa',,'Trung',12.25,109.19],['Trường Sa',,'Trung',10.2,113.4],['Ninh Thuận',,'Trung',11.57,109.0],['Bình Thuận',,'Trung',10.93,108.1],
 ['Kon Tum',,'TayNguyen',14.6,107.8],['Gia Lai',,'TayNguyen',13.8,108.0],['Đắk Lắk',,'TayNguyen',12.75,108.2],['Đắk Nông',,'TayNguyen',12.1,107.6],
 ['Lâm Đồng',,'TayNguyen',11.7,108.3],
 ['Bình Phước',,'Nam',11.75,106.8],['Tây Ninh',,'Nam',11.31,106.0],['Bình Dương',,'Nam',11.1,106.6],['Đồng Nai',,'Nam',11.05,107.15],
 ['Bà Rịa – Vũng Tàu','Vũng Tàu','Nam',10.45,107.25],['TP. Hồ Chí Minh','TP.HCM','Nam',10.75,106.7],['Long An',,'Nam',10.75,106.2],
 ['Tiền Giang',,'Nam',10.38,106.36],['Bến Tre',,'Nam',10.15,106.5],['Trà Vinh',,'Nam',9.85,106.34],['Vĩnh Long',,'Nam',10.2,105.97],
 ['Đồng Tháp',,'Nam',10.55,105.63],['An Giang',,'Nam',10.45,105.13],['Kiên Giang',,'Nam',10.0,104.9],['Cần Thơ',,'Nam',10.03,105.6],
 ['Hậu Giang',,'Nam',9.78,105.47],['Sóc Trăng',,'Nam',9.6,105.97],['Bạc Liêu',,'Nam',9.29,105.72],['Cà Mau',,'Nam',9.0,105.15],
];
const VEN = ['Quảng Ninh','Hải Phòng','Thái Bình','Nam Định','Ninh Bình','Thanh Hóa','Nghệ An','Hà Tĩnh','Quảng Bình','Quảng Trị','Thừa Thiên Huế','Đà Nẵng','Quảng Nam','Quảng Ngãi','Bình Định','Phú Yên','Khánh Hòa','Ninh Thuận','Bình Thuận','Bà Rịa – Vũng Tàu','TP. Hồ Chí Minh','Tiền Giang','Bến Tre','Trà Vinh','Sóc Trăng','Bạc Liêu','Cà Mau','Kiên Giang'];
const NUI = ['Hà Giang','Cao Bằng','Bắc Kạn','Lạng Sơn','Tuyên Quang','Thái Nguyên','Phú Thọ','Bắc Giang','Lào Cai','Yên Bái','Lai Châu','Điện Biên','Sơn La','Hòa Bình'];
const BTB = ['Thanh Hóa','Nghệ An','Hà Tĩnh','Quảng Bình','Quảng Trị','Thừa Thiên Huế'];
const pts = P.map(([n,s,r,lat,lon]) => ({n,s,r,x:(lon-102)*110,y:(23.6-lat)*105, ox:(lon-102)*110, oy:(23.6-lat)*105, fixed: n==='Hoàng Sa'||n==='Trường Sa'}));
const MIN = 76;
for (let it=0; it<2000; it++) {
  for (let i=0;i<pts.length;i++) for (let j=i+1;j<pts.length;j++) {
    const a=pts[i], b=pts[j]; let dx=b.x-a.x, dy=b.y-a.y; let d=Math.hypot(dx,dy)||0.01;
    if (d<MIN) { const k=(MIN-d)/2/d; if(!a.fixed){a.x-=dx*k;a.y-=dy*k;} if(!b.fixed){b.x+=dx*k;b.y+=dy*k;} }
  }
  for (const p of pts) if(!p.fixed){ p.x += (p.ox-p.x)*0.004; p.y += (p.oy-p.y)*0.004; }
}
const out = pts.map(p => ({name:p.n, ...(p.s?{short:p.s}:{}), region:p.r,
  tags:[...(VEN.includes(p.n)?['VenBien']:[]),...(p.fixed?['Dao']:[]),...(NUI.includes(p.n)?['MienNuiPhiaBac']:[]),...(BTB.includes(p.n)?['BacTrungBo']:[])],
  x:Math.round(p.x), y:Math.round(p.y)}));
fs.writeFileSync(process.argv[2], JSON.stringify(out, null, 1));
console.log(out.length, Math.min(...out.map(o=>o.x)), Math.max(...out.map(o=>o.x)), Math.min(...out.map(o=>o.y)), Math.max(...out.map(o=>o.y)));
