// Gộp content/parts/q-*.json → Data/questions.json, s-*.json → Data/seasonal-questions.json
const fs = require('fs'), path = require('path');
const dir = 'content/parts', out = 'src/HanhTrinh.Web/Data';
const load = pre => fs.readdirSync(dir).filter(f => f.startsWith(pre) && f.endsWith('.json')).sort()
  .flatMap(f => JSON.parse(fs.readFileSync(path.join(dir, f), 'utf8')));
const q = load('q-'), s = load('s-');
fs.writeFileSync(path.join(out, 'questions.json'), JSON.stringify(q, null, 1));
fs.writeFileSync(path.join(out, 'seasonal-questions.json'), JSON.stringify(s, null, 1));
console.log('questions', q.length, 'seasonal', s.length);
