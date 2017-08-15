<?php
include('overbook.inc.php');

function pushbulletNotification($access_token, $title, $message)
{
    $post_data = array(
        'type' => 'note',
        'title' => $title,
        'body' => $message
    );

    $json_data = json_encode($post_data);
    $ch = curl_init("https://api.pushbullet.com/v2/pushes");

    // Prepare our JSON header
    $headers = array(
        'Content-Type: application/json',
        'Content-Length: ' . strlen($json_data),
        'Access-Token: ' . $access_token
    );

    // set options to send JSON
    curl_setopt($ch, CURLOPT_POST, 1);
    curl_setopt($ch, CURLOPT_RETURNTRANSFER, true);
    curl_setopt($ch, CURLOPT_HTTPHEADER, $headers);
    curl_setopt($ch, CURLOPT_POSTFIELDS, $json_data);
    $resp = curl_exec($ch);
    curl_close($ch);
}

function writeConfig($whitelist, $blacklist, $requests)
{
    $newdata = array(
        "whitelist" => $whitelist,
        "blacklist" => $blacklist,
        "requests" => $requests
    );
    file_put_contents("overbook.json", json_encode($newdata));
}

function startsWith($haystack, $needle)
{
    return $haystack[0] === $needle[0]
        ? strncmp($haystack, $needle, strlen($needle)) === 0
        : false;
}

function curlHeaderCallback($resURL, $strHeader)
{
    if(startsWith($strHeader, "HTTP") || startsWith($strHeader, 'Content-'))
        header($strHeader);
    return strlen($strHeader); 
}

function curlWriteCallback($curl, $data)
{
    echo $data;
    return strlen($data);
}

function getWebdavFile($urlbase, $basicauth, $file)
{
    set_time_limit(0);
    header('Cache-Control: no-store, no-cache, must-revalidate');
    header('Pragma: no-cache');
    $fullurl = $urlbase . rawurlencode($file);
    $ch = curl_init();
    curl_setopt($ch, CURLOPT_URL, $fullurl);
    curl_setopt($ch, CURLOPT_USERPWD, $basicauth);
    curl_setopt($ch, CURLOPT_RETURNTRANSFER, true);
    curl_setopt($ch, CURLOPT_HEADERFUNCTION, 'curlHeaderCallback');
    curl_setopt($ch, CURLOPT_WRITEFUNCTION, 'curlWriteCallback');
    curl_exec($ch);
    curl_close($ch);
}

$data = json_decode(file_get_contents("overbook.json"), true);
$whitelist = $data["whitelist"];
$blacklist = $data["blacklist"];
$requests = $data["requests"];

if(isset($_GET["file"]) && !empty($_GET["file"]))
{
    $ip = $_SERVER['REMOTE_ADDR'];
    $reqfile = $_GET["file"];
    if(strpos($reqfile, '/') == false || (is_array($whitelist) && in_array($ip, $whitelist)))
    {
        if(strstr($reqfile, '..'))
            http_response_code(403);
        else
            getWebdavFile($cdn_urlbase, $cdn_basicauth, $reqfile);
    }
    else if(is_array($blacklist) && in_array($ip, $blacklist))
        http_response_code(403);
    else
    {
        http_response_code(403);
        foreach($requests as $request)
            if($request == $ip)
                die;
        $phpurl = $_SERVER["HTTP_HOST"].explode('?', $_SERVER["REQUEST_URI"], 2)[0];
        $token = bin2hex(openssl_random_pseudo_bytes(8));
        pushbulletNotification($pushbullet_key, sprintf('Overbook Request (%s)', $ip), sprintf("File:\n%s\n\nApprove:\nhttp://%s?approve=%s\n\nDeny:\nhttp://%s?deny=%s", $reqfile, $phpurl, $token, $phpurl, $token));
        $requests[$token] = $ip;
        writeConfig($whitelist, $blacklist, $requests);
    }
}
else if(isset($_GET["approve"]) && !empty($_GET["approve"]))
{
    header('Content-Type: text/txt');
    $token = $_GET["approve"];
    if(!isset($requests[$token]))
    {
        http_response_code(404);
        die;
    }
    $ip = $requests[$token];
    unset($requests[$token]);
    if(!empty($ip))
    {
        if(!is_array($whitelist))
            $whitelist = array();
        array_push($whitelist, $ip);
        echo sprintf("whitelisted %s", $ip);
    }
    else
    {
        http_response_code(404);
    }
    writeConfig($whitelist, $blacklist, $requests);
}
else if(isset($_GET["deny"]) && !empty($_GET["deny"]))
{
    header('Content-Type: text/txt');
    $token = $_GET["deny"];
    if(!isset($requests[$token]))
    {
        http_response_code(404);
        die;
    }
    $ip = $requests[$token];
    unset($requests[$token]);
    if(!empty($ip))
    {
        if(!is_array($blacklist))
            $blacklist = array();
        array_push($blacklist, $ip);
        echo sprintf("blacklisted %s", $ip);
    }
    else
    {
        http_response_code(404);
    }
    writeConfig($whitelist, $blacklist, $requests);
}
?>