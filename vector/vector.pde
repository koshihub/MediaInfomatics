
PVector xaxis, yaxis, target;
float s, t;

void setup() {
  size(640,480);
  
  xaxis = new PVector(1.0, 0, 0);
  yaxis = new PVector(0, 1.0, 0);
  target = new PVector(0.3, 0.5, 0);
  
  PVector sv, tv;
  sv = PVector.mult( target, PVector.angleBetween(xaxis, target) );
  tv = PVector.mult( target, PVector.angleBetween(yaxis, target) );
  
  println("x: "+sv.mag());
  println("y: "+tv.mag());
}

void draw()
{
  background(255);
  line(0, 0, 10, 10);
}

