<?xml version="1.0" encoding="UTF-8"?>
<tileset version="1.5" tiledversion="1.7.1" name="rainforest jungle v1" tilewidth="16" tileheight="16" tilecount="256" columns="16">
 <image source="rainforest jungle v1.png" width="256" height="256"/>
 <tile id="71">
  <animation>
   <frame tileid="0" duration="800"/>
   <frame tileid="71" duration="150"/>
   <frame tileid="72" duration="150"/>
   <frame tileid="73" duration="200"/>
   <frame tileid="74" duration="100"/>
  </animation>
 </tile>
 <tile id="72">
  <animation>
   <frame tileid="71" duration="150"/>
   <frame tileid="72" duration="150"/>
   <frame tileid="73" duration="200"/>
   <frame tileid="74" duration="100"/>
   <frame tileid="0" duration="800"/>
  </animation>
 </tile>
 <tile id="73">
  <animation>
   <frame tileid="73" duration="200"/>
   <frame tileid="74" duration="100"/>
   <frame tileid="0" duration="800"/>
   <frame tileid="71" duration="150"/>
   <frame tileid="72" duration="150"/>
  </animation>
 </tile>
 <wangsets>
  <wangset name="Dirt on grass" type="edge" tile="-1">
   <wangcolor name="Dirt" color="#ff0000" tile="-1" probability="1"/>
   <wangtile tileid="3" wangid="0,0,1,0,1,0,0,0"/>
   <wangtile tileid="4" wangid="0,0,1,0,1,0,1,0"/>
   <wangtile tileid="5" wangid="0,0,0,0,1,0,1,0"/>
   <wangtile tileid="6" wangid="1,0,1,0,1,0,1,0"/>
   <wangtile tileid="19" wangid="1,0,1,0,1,0,0,0"/>
   <wangtile tileid="21" wangid="1,0,0,0,1,0,1,0"/>
   <wangtile tileid="22" wangid="1,0,1,0,1,0,1,0"/>
   <wangtile tileid="35" wangid="1,0,1,0,0,0,0,0"/>
   <wangtile tileid="36" wangid="1,0,1,0,0,0,1,0"/>
   <wangtile tileid="37" wangid="1,0,0,0,0,0,1,0"/>
   <wangtile tileid="38" wangid="1,0,1,0,1,0,1,0"/>
   <wangtile tileid="51" wangid="1,0,0,0,1,0,1,0"/>
   <wangtile tileid="52" wangid="0,0,0,0,0,0,1,0"/>
   <wangtile tileid="53" wangid="0,0,0,0,1,0,0,0"/>
   <wangtile tileid="54" wangid="1,0,1,0,1,0,1,0"/>
   <wangtile tileid="67" wangid="1,0,1,0,1,0,0,0"/>
   <wangtile tileid="68" wangid="0,0,1,0,0,0,0,0"/>
   <wangtile tileid="69" wangid="1,0,0,0,0,0,0,0"/>
   <wangtile tileid="70" wangid="1,0,1,0,1,0,1,0"/>
  </wangset>
  <wangset name="Water on dirt" type="corner" tile="-1">
   <wangcolor name="Water" color="#ff0000" tile="-1" probability="1"/>
   <wangtile tileid="7" wangid="0,1,0,0,0,1,0,1"/>
   <wangtile tileid="8" wangid="0,1,0,0,0,0,0,1"/>
   <wangtile tileid="9" wangid="0,1,0,0,0,0,0,1"/>
   <wangtile tileid="10" wangid="0,1,0,1,0,0,0,1"/>
   <wangtile tileid="23" wangid="0,0,0,0,0,1,0,1"/>
   <wangtile tileid="24" wangid="0,0,0,1,0,0,0,0"/>
   <wangtile tileid="25" wangid="0,0,0,0,0,1,0,0"/>
   <wangtile tileid="26" wangid="0,1,0,1,0,0,0,0"/>
   <wangtile tileid="39" wangid="0,0,0,0,0,1,0,1"/>
   <wangtile tileid="40" wangid="0,1,0,0,0,0,0,0"/>
   <wangtile tileid="41" wangid="0,0,0,0,0,0,0,1"/>
   <wangtile tileid="42" wangid="0,1,0,1,0,0,0,0"/>
   <wangtile tileid="43" wangid="0,1,0,0,0,1,0,0"/>
   <wangtile tileid="55" wangid="0,0,0,1,0,1,0,1"/>
   <wangtile tileid="56" wangid="0,0,0,1,0,1,0,0"/>
   <wangtile tileid="57" wangid="0,0,0,1,0,1,0,0"/>
   <wangtile tileid="58" wangid="0,1,0,1,0,1,0,0"/>
   <wangtile tileid="59" wangid="0,0,0,1,0,0,0,1"/>
   <wangtile tileid="86" wangid="0,1,0,1,0,1,0,1"/>
   <wangtile tileid="101" wangid="0,1,0,1,0,1,0,1"/>
   <wangtile tileid="102" wangid="0,1,0,1,0,1,0,1"/>
   <wangtile tileid="117" wangid="0,1,0,1,0,1,0,1"/>
   <wangtile tileid="118" wangid="0,1,0,1,0,1,0,1"/>
  </wangset>
  <wangset name="Clifftop" type="corner" tile="-1">
   <wangcolor name="Grass" color="#ff0000" tile="-1" probability="1"/>
  </wangset>
 </wangsets>
</tileset>
