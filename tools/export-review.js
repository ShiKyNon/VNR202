// Xuất docs/questions-review.md từ Data/*.json để duyệt câu hỏi.
const fs = require('fs');
const q = JSON.parse(fs.readFileSync('src/HanhTrinh.Web/Data/questions.json', 'utf8'));
const s = JSON.parse(fs.readFileSync('src/HanhTrinh.Web/Data/seasonal-questions.json', 'utf8'));
const L = 'ABCD', season = { Xuan: 'Xuân', Ha: 'Hạ', Thu: 'Thu', Dong: 'Đông' };
const fmt = x => `### ${x.id} · tr.${x.page}\n\n**${x.text}**\n\n${x.options.map((o, i) => `- ${L[i]}. ${o}${i === x.answer ? ' ✔' : ''}`).join('\n')}\n\n> ${x.explanation}\n`;
let md = `# Bộ câu hỏi — bản duyệt\n\nTổng: ${q.length} câu chung + ${s.length} câu tỉnh phát sáng. ✔ = đáp án đúng.\n\n`;
const ch = ['Chương nhập môn', 'Chương 1 (1930–1945)', 'Chương 2 (1945–1975)', 'Chương 3 (1975–nay) và Kết luận'];
for (let c = 0; c < 4; c++) { const part = q.filter(x => x.chapter === c); md += `## ${ch[c]} — ${part.length} câu\n\n` + part.map(fmt).join('\n'); }
md += `\n## Câu tỉnh phát sáng\n\n`;
for (const k of Object.keys(season)) { const part = s.filter(x => x.season === k); md += `### Mùa ${season[k]}\n\n` + part.map(x => fmt({ ...x, id: `${x.id} · ${x.province}` })).join('\n'); }
fs.writeFileSync('docs/questions-review.md', md);
console.log('ok', q.length, s.length);
