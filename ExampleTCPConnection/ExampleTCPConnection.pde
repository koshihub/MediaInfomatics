import java.util.*;
import processing.net.*;

String HOST = "127.0.0.1";
int PORT = 8890;
byte[] sendTextBytes = new byte[1];
String sendText;
Client client;
String textPool = "";
String rcvText = "";

void setup()
{
  size(640, 480, P3D);
  client = new Client(this, HOST, PORT);
  PFont font;
  font = loadFont("ArialMT-24.vlw");
  textFont(font);
}

float tx, ty;

void draw()
{
  PVector targetPos = updateTCPData();
  if (targetPos.x != -100 && targetPos.y != -100) {
    tx = targetPos.x;
    ty = targetPos.y;
  }
  background(200);
  fill(0);
  text("(tx, ty) = (" + tx + "," + ty + ")", 50, 50);
}

// 受信
PVector updateTCPData()
{
  float tx = -100;
  float ty = -100;
  while (client.available() > 0)
  {
    // 何かデータが送られてきた
    try
    {
      // クエリの取得
      rcvText += client.readString();
      if (rcvText.charAt(rcvText.length() - 1) != ';') continue;
      rcvText = rcvText.substring(0, rcvText.length() - 1);
      String[] texts = rcvText.split(" ");
      rcvText = "";
      tx = Float.parseFloat(texts[0]);
      ty = Float.parseFloat(texts[1]);
      sendText("s");
    }
    catch (Exception e)
    {
      println(e);
    }
  }
  return new PVector(tx, ty);
}
// 送信
void sendText(String text)
{
  int start = millis();
  for (int i = 0; i < text.length(); i++)
  {
    sendTextBytes[i] = (byte)text.charAt(i);
  }
  Arrays.fill(sendTextBytes, text.length(), sendTextBytes.length, (byte)0);
  //println("start sending text \"" + text + "\"");
  client.write(sendTextBytes);
  //println("done sending text (elapsed " + (millis() - start) + " ms");
}
