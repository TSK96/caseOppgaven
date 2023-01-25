const http = require('http');
const fs = require('fs');
const csv = require('csv-parser')

const hostname = '127.0.0.1';
const port = 3000;
const results = [];
const path = 'po-kunder-with-data.csv';
let htmlpage = '';

fs.createReadStream(path)
.pipe(csv({ separator: ';' }))
.on('data', (data) => results.push(data))
.on('end', () => {
  stats();
});

const server = http.createServer((req, res) => {
  res.statusCode = 200;
  res.end(htmlpage);
});

server.listen(port, hostname, () => {
  console.log(`Server running at http://${hostname}:${port}/`);
});

function flexitem(title, data, percent){
  return `
    <div>
      <div style="
        border:1px solid black;
        padding: 0 5px 5px 5px;
      ">
        <h3>${title}</h3>
        <div>Antall: <b>${data}</b></div>
        <div><b>${percent} %</b></div>
      </div>
    </div>
    `;
}

function stats(){

  const empty = results.filter(bedrift => bedrift.AntallAnsatte == 0).length;
  const konkurs = results.filter(bedrift => bedrift.Status == 'konkurs: True').length;
  const sletted = results.filter(bedrift => bedrift.Status.substr(1,9) == 'slettdato').length;

  const ENK = (results.filter(bedrift => bedrift.Organisasjonsform == 'ENK')).length;
  const AS = (results.filter(bedrift => bedrift.Organisasjonsform == 'AS')).length;
  const ANDRE = (results.filter(bedrift => bedrift.Organisasjonsform != 'ENK' && bedrift.Organisasjonsform != 'AS')).length;
  const total = results.length;

  let antAnsatte = [];
  for(i=0; i<results.length; i++){
    if(results[i].AntallAnsatte > 0){
      const idx = antAnsatte.findIndex(bed => bed.AntallAnsatte == results[i].AntallAnsatte);
      if(idx == -1){
        antAnsatte.push({AntallAnsatte: results[i].AntallAnsatte, AntallBedrifter: 1});
      } else {
        antAnsatte[idx].AntallBedrifter = antAnsatte[idx].AntallBedrifter + 1;
      }
    }
  }

  const sorted = antAnsatte.sort((a, b) => {return a.AntallAnsatte - b.AntallAnsatte});
 
  let html = `
  <html> 
    <style>
      table, th, td { border:1px solid black; padding: 5px;}
      table { width:100%;}
    </style>
    <body>`;

  html += `
    <div style="
      display: flex;
      justify-content: center;
      gap: 10px;
      padding: 20px;
    ">
      ${flexitem('ENK bedrifter:', ENK, (ENK/total * 100).toFixed(2))}
      ${flexitem('AS bedrifter:', AS, (AS/total * 100).toFixed(2))}
      ${flexitem('ANDRE bedrifter:', ANDRE, (ANDRE/total * 100).toFixed(2))}
      ${flexitem('Bedrifter uten ansatte:', empty, (empty/total * 100).toFixed(2))}
      ${flexitem('Konkurse bedrifter:', konkurs, (konkurs/total * 100).toFixed(2))}
      ${flexitem('Sletted bedrifter:', sletted, (sletted/total * 100).toFixed(2))}

    </div>
  `;
  
  html += '<table><tr>';

  html += '<th><b>Antall ansatte</b></th>';

  html +=   sorted.reduce((a,ansatte) => {
    return a + ('<th>' + ansatte.AntallAnsatte + '</th>')
  }, '');

  html += '</tr><tr>';

  html += '<td><b>Antall bedrifter</b></td>';

  html +=   sorted.reduce((a,ansatte) => {
    return a + ('<td>' + ansatte.AntallBedrifter + '</td>')
  }, '');

  html += '</tr></table></body></html>';

  htmlpage = html;
 
}
