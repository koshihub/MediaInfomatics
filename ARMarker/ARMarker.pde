import processing.video.*;
import jp.nyatla.nyar4psg.*;
import java.awt.Frame;

Capture cam;
MultiMarker nya;

SecondApplet app;
PFrame frame;

boolean isField = false;
boolean[] isTarget = {false, false};
PVector[] target = {new PVector(), new PVector()};
PVector[] st = {new PVector(), new PVector()};

void setup() {
  size(640,480,P3D);
  colorMode(RGB, 100);
  println(MultiMarker.VERSION);
  
  String[] cameras = Capture.list();
  for(String a : cameras) println(a);
  cam = new Capture(this, cameras[1]);
  
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
      
      PVector targetPos = updateTCPData();
      float tx, ty;
      if (targetPos.x != -100 && targetPos.y != -100) {
        tx = targetPos.x;
        ty = targetPos.y;
      } else {
        ellipse(tx*300+10, ty*300+10, 5, 5);
      }
    }
} 

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
