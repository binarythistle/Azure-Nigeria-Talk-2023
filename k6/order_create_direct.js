import http from 'k6/http';


export let options = {
    vus: 1,
    //duration: '30s',
    iterations: 100
};

export default function () {

    var url = "<INSERT YOUR AZURE HTTP TRIGGER FUNCTION URL>";

    var payload = JSON.stringify({ name: "Les Jackson" });

    var params = {
        headers: {
            'Content-Type': 'application/json',
            'user-agent': 'k6-webhook-test',
            'authkey' : 'abc123',
        }
    };



    var res = http.post(url, payload, params);
    console.log('Response Code was ' + String(res.status) 
        + ' / Response Time: ' + String(res.timings.duration) + ' ms'
        + ' / Response : ' + String(res.body)
    );
}