import processing.net.*;
  
Client client;
int speed_L = 0;
int speed_R = 0;
  
void setup() {
  // Locomoに接続
  client = new Client(this, "172.20.10.3", 9750);
}
  
void draw() {
  // 特に何もしない
}
 
// キーが押された時の処理（モータ回転）
void keyPressed() {
  if ( keyCode==UP ) {
    speed_L = 255;
    speed_R = 255;
  }
  if ( keyCode==DOWN ) {
    speed_L = -255;
    speed_R = -255;
  }
  if ( keyCode==LEFT ) {
    speed_L = -255;
    speed_R = 255;
  }
  if ( keyCode==RIGHT ) {
    speed_L = 255;
    speed_R = -255;
  }
  client.write("L" + speed_L + " R" + speed_R + "\r");
}
 
// キーが離された時の処理（モータ停止）
void keyReleased() {
  speed_L = 0;
  speed_R = 0;
  client.write("L" + speed_L + " R" + speed_R + "\r");
}
