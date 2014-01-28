import processing.video.*;
import jp.nyatla.nyar4psg.*;
import java.awt.Frame;
import processing.net.*;
import java.util.*;
import processing.serial.*;

final boolean kinectMode = true;

//Serial locomo;
Client locomo;

int speed_L = 0;
int speed_R = 0;
  
Capture cam;
MultiMarker nya;

SecondApplet app;
PFrame frame;

boolean isField = false;
boolean[] isTarget = {false, false};
PVector[] target = {new PVector(), new PVector()};
PVector[] st = {new PVector(), new PVector()};

PVector targetPos;
    
void connectLocomo() {
  if (locomo != null) locomo.stop();
  println("connecting to locomo...");
  //locomo = new Serial(this,locomo"COM3",9600);
  locomo = new Client(this, "172.20.10.5", 9750);
  if( locomo != null ) println("connected to locomo!");
}

void setup() {
  size(640,480,P3D);
  connectLocomo();
  
  client = new Client(this, HOST, PORT);
  colorMode(RGB, 100);
  println(MultiMarker.VERSION);
  
  String[] cameras = Capture.list();
  //for(String a : cameras) println(a);
  cam = new Capture(this, cameras[25]);
  
  targetPos = new PVector(-100, -100);
  
  nya=new MultiMarker(this,width,height,"camera_para.dat",NyAR4PsgConfig.CONFIG_PSG);
  for(int i=0; i<6; i++) {
    nya.addARMarker("marker16_" + (i+1) + ".pat", 80);
  }
  cam.start();
  
  // open second frame
  frame = new PFrame();
}

void draw()
{
  if (cam.available() !=true) {
      return;
  }
  cam.read();
  nya.detect(cam);
  background(0);
  nya.drawBackground(cam);
  
  // axises
  if( nya.isExistMarker(0) &&
      nya.isExistMarker(1) &&
      nya.isExistMarker(2) ) 
  {
    isField = true;
    
    // get position of three points
    PVector zero = new PVector(0.0, 0.0, 0.0);
    PVector origin, xp, yp;
    origin = new PVector();
    xp = new PVector();
    yp = new PVector();
    nya.getMarkerMatrix(0).mult(zero, origin);
    nya.getMarkerMatrix(1).mult(zero, xp);
    nya.getMarkerMatrix(2).mult(zero, yp);
    
    // get two axises
    PVector xaxis, yaxis;
    xaxis = new PVector(xp.x-origin.x, xp.y-origin.y, xp.z-origin.z);
    yaxis = new PVector(yp.x-origin.x, yp.y-origin.y, yp.z-origin.z);
    
    /*
    PMatrix3D origin_mat = nya.getMarkerMatrix(0);
    if (origin_mat.invert()) {
      PVector x, y;
      x = new PVector();
      y = new PVector();
      origin_mat.mult(xaxis, x);
      origin_mat.mult(yaxis, y);
      
      nya.beginTransform(0);
      // draw axises
      stroke(255, 0, 0);
      strokeWeight(2);
      line(0,0,0,x.x/2.0f,x.y/2.0f,x.z/2.0f);
      line(0,0,0,y.x/2.0f,y.y/2.0f,y.z/2.0f);
      nya.endTransform();
    }
    */
    
    // target
    for(int i=0; i<2; i++) {
      PVector start = new PVector();
      PVector end = new PVector();
      PVector dir = new PVector(1.0, 0.0, 0.0);
      if( nya.isExistMarker(3+i) ) {
        isTarget[i] = true;
        nya.getMarkerMatrix(3+i).mult(zero, start);
        nya.getMarkerMatrix(3+i).mult(dir, end);
          
        /*
        PVector pos = new PVector();
        origin_mat.mult(start, pos);
        nya.beginTransform(0);
        // draw axises
        stroke(255, 0, 0);
        strokeWeight(2);
        line(0,0,0,pos.x,pos.y,pos.z);
        nya.endTransform();
        */
        
        st[i] = getST(xaxis, yaxis, 
                   new PVector(start.x-origin.x,start.y-origin.y,start.z-origin.z));
        PVector endst = getST(xaxis, yaxis, 
                   new PVector(end.x-origin.x,end.y-origin.y,end.z-origin.z));
        target[i].set(endst.x - st[i].x, endst.y - st[i].y);
        target[i].normalize();
      } else {
        isTarget[i] = false;
      }
    }
  } else {
    isField = false;
  }
}

PVector getST(PVector xaxis, PVector yaxis, PVector target) {
  PVector sv, tv;
  sv = PVector.mult( target, cos(PVector.angleBetween(xaxis, target)) );
  tv = PVector.mult( target, cos(PVector.angleBetween(yaxis, target)) );
  
  return new PVector(sv.mag() / xaxis.mag(), tv.mag() / yaxis.mag());
}

public class PFrame extends Frame {
    public PFrame() {
        setBounds(100,100,400,400);
        app = new SecondApplet();
        add(app);
        app.init();
        show();
    }
}

public class SecondApplet extends PApplet {
    public void setup() {
        size(320, 400);
    }

    public void draw() {
      if (isField) {
        background(255);
      } else {
        background(0);
      }
      line(10, 10, 310, 10);
      line(10, 10, 10, 310);
      for(int i=0; i<2; i++) {
        if(isTarget[i]) {
          float posx = st[i].x * 300, posy = st[i].y * 300;
          line(posx + 10, posy + 10, posx + target[i].x*20 + 10, posy + target[i].y*20 + 10);
        }
      }
      
      PVector temp = updateTCPData();
      if( !(temp.x < -50 || temp.y < -50) ) {
        float tx, ty;
        targetPos = temp;
        tx = targetPos.x;
        ty = targetPos.y;
        ellipse(tx*300+10, ty*300+10, 5, 5);
      }
      
      if( !locomo.active() ) {
        connectLocomo();
      } else {
        updateLocomo();
      }
    }

    void updateLocomo()
    {
      float mx = 0;
      float my = 0;
      int i = 0;
      
      if (kinectMode) {
        if (targetPos.x < -50 || targetPos.y < -50) {
            println("stop!");
            locomo.write("L" + (0) + " R" + (0) + "\r");
            locomo.write("L" + (0) + " R" + (0) + "\r");
            locomo.write("L" + (0) + " R" + (0) + "\r");
            return;
        } else {
            mx = targetPos.x;
            my = targetPos.y;
        }
        if (!isTarget[i] || !isField) {
          println("stop!");
          locomo.write("L" + (0) + " R" + (0) + "\r");
          locomo.write("L" + (0) + " R" + (0) + "\r");
          locomo.write("L" + (0) + " R" + (0) + "\r");
          return;
        }
      }
      else {
        if (!isTarget[i] || !isField) {
          locomo.write("L" + (0) + " R" + (0) + "\r");
          locomo.write("L" + (0) + " R" + (0) + "\r");
          locomo.write("L" + (0) + " R" + (0) + "\r");
          return;
        }
        mx = (mouseX - 10) / 300f;
        my = (mouseY - 10) / 300f; 
      }
      
      println ("mx = " + mx + ", my = " + my);
      
      if (mx < -1 || 2 < mx) return;
      if (my < -1 || 2 < my) return;
      float dirX = mx - st[i].x;
      float dirY = my - st[i].y;
      float len = sqrt(dirX * dirX + dirY * dirY);
      dirX /= len;
      dirY /= len;
      
      // get angle between dir and target[i]
      PVector vec1 = new PVector(target[i].x, target[i].y, 0);
      PVector vec2 = new PVector(dirX, dirY, 0);
      float angle = (float)degrees(PVector.angleBetween(vec1, vec2));
      final float thresAngle = 30f;
      if (abs(angle) <= thresAngle) {
        locomo.write("L" + (-255) + " R" + (-255) + "\r");
      }
      else if (angle > 0) {
        locomo.write("L" + (5) + " R" + (-5) + "\r");
      }
      else {
        locomo.write("L" + (-5) + " R" + (5) + "\r");
      }
  }
} 

String HOST = "127.0.0.1";
int PORT = 8890;
byte[] sendTextBytes = new byte[1];
String sendText;
Client client;
String textPool = "";
String rcvText = "";
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
