﻿// Functional Fonts by terryspitz
// Mar 2020

//TODOs:
//- move outline point inward only
//- fixed-width font
//- serifs
//- make variable font
//- join lines properly
//- horiz/vertical endcaps
//- correct tight bend in 5
//- remove overlap in SVG
//- render animation
//- try merging with https://magenta.tensorflow.org/svg-vae
//- add punctuation chars

//Features :
// Backscratch font (made of 4 parallel lines)
// Generated FontForge fonts

module Generator


open System

open SpiroPointType
open SpiroSegment
open SpiroControlPoint
open PathBezierContext

// Variable values for the font
type Axes = {
    width : int     //width of normal glyph
    height : int    //capital height
    x_height : int  //height of lower case
    offset : int    //roundedness
    thickness : int //stroke width
    leading : int   //gap between glyphs
    italic_fraction : float
    outline : bool
    stroked : bool    //each stroke is 4 parallel lines
    scratches : bool //horror font
    filled : bool   //(svg only) filled or empty outlines
} with
    static member DefaultAxes = { 
        width = 300; height = 600; x_height = 400; offset = 100; thickness = 3; italic_fraction = 0.0; leading = 50;
        outline = true; stroked = false; scratches = false; filled = true; }

type Point =
    // Raw coordinates
    | YX of y: int * x: int

    // Y coordinate: Top,X-height,Half-height,Bottom
    // X coordinate: Left,Centre,Right
    // o adds/subtracts an offset to the dimension it follows
    | TL | TLo | TC | TR        // Top points: Left, Left offset inward, Centre, Right
    | ToL | ToC | ToR           // Top offset down
    | XL | XLo | XC | XRo | XR  // x-height
    | XoL | XoC | XoR           // x-height offset down
    | ML | MC | MR              // Midway down from x-height
    | HL | HLo | HC | HRo | HR  // half glyph height
    | HoR                       // half offset down
    | BoL | BoC | BoR           // Bottom offset up
    | BL | BLo | BC | BRo | BR  // Bottom
    | DoL                       // Descender offset up
    | DL | DC | DR              // Descender
    | BN | BoN | HN | XoN | XN | TN         // Narrow width points
    | Mid of p1 : Point * p2 : Point
    | Interp of p1 : Point * p2 : Point * frac : float
    //member this.(+) y x = YX(this.getXY+y, this.x+x)

type SCP = SpiroControlPoint

type Element = 
    | Glyph of c : char
    | Part of name : string
    | Line of p1: Point * p2: Point
    | PolyLine of list<Point>
    | OpenCurve of list<Point * SpiroPointType>
    | ClosedCurve of list<Point * SpiroPointType>
    | Dot of Point
    | EList of list<Element>
    | Space

type SpiroElement =
    | SpiroOpenCurve of scps: list<SCP> * segments: list<SpiroSegment>
    | SpiroClosedCurve of scps: list<SCP> * segments: list<SpiroSegment>
    | SpiroDot of Point
    | SpiroSpace

let CurveToLine = SpiroPointType.Left
let LineToCurve = SpiroPointType.Right
let G2 = SpiroPointType.G2
let G4 = SpiroPointType.G4
let Start = SpiroPointType.OpenContour
let Corner = SpiroPointType.Corner
let End = SpiroPointType.EndOpenContour
let EndClosed = SpiroPointType.End

type SpiroSegment with 
    member this.Offset theta dist = YX(int(this.Y + dist * sin(theta)), int(this.X + dist * cos(theta)))

let offsetPoint X Y theta dist =
    YX(int(Y + dist * sin(theta)), int(X + dist * cos(theta)))

let PI = Math.PI        
let concatLines = String.concat "\n"
let svgCircle x y r =
    sprintf "M %d,%d\n" (x-r) y +
    sprintf "C %d,%d %d,%d %d,%d\n" (x-r) (y+r/2) (x-r/2) (y+r) x (y+r) +
    sprintf "C %d,%d %d,%d %d,%d\n" (x+r/2) (y+r) (x+r) (y+r/2) (x+r) y +
    sprintf "C %d,%d %d,%d %d,%d\n" (x+r) (y-r/2) (x+r/2) (y-r) x (y-r) +
    sprintf "C %d,%d %d,%d %d,%d\n" (x-r/2) (y-r) (x-r) (y-r/2) (x-r) y +
    "Z" 


//class
type Font (axes: Axes) =

    // X axis guides, from left
    let L = 0               // Left
    let R = axes.width      // Right = standard glyph width
    let N = R * 4/5         // Narrow glyph width
    let C = R / 2           // Centre

    // Y axis guides, from bottom-up
    let B = 0               // Bottom
    let X = axes.x_height   // x-height
    let M = X/2             // Midway down from x-height
    let T = axes.height     // Top = standard glyph caps height
    let H = T/2             // Half total height
    let D = -axes.height/2  // descender height
    let offset = axes.offset // offset from corners
    let dotHeight = max ((X+T)/2) (X+axes.thickness*3)

    member this.Axes = axes
    
    member this.rewritePoint p = 
        match p with
        | YX(y,x) -> (y,x)
        | TL -> (T,L) | TLo -> (T,L+offset) | TC -> (T,C) | TR -> (T,R)
        | ToL -> (T-offset,L) | ToC -> (T-offset,C) | ToR -> (T-offset,R)
        | XL -> (X,L) | XLo -> (X,L+offset) | XC -> (X,C) | XRo -> (X,R-offset) | XR -> (X,R)
        | XoL -> (X-offset,L) | XoC -> (X-offset,C) | XoR -> (X-offset,R)
        | ML -> (M,L) | MC -> (M,C) | MR -> (M,R)
        | HL -> (H,L) | HLo -> (H,L+offset) | HC -> (H,C) | HRo -> (H,R-offset) | HR -> (H,R)
        | HoR -> (H-offset,R)
        | BoL -> (B+offset,L) | BoC -> (B+offset,C) | BoR -> (B+offset,R)
        | BL -> (B,L) | BLo -> (B,L+offset) | BC -> (B,C) | BRo -> (B,R-offset) | BR -> (B,R)
        | DoL -> (D+offset,L)
        | DL -> (D,L) | DC -> (D,C) | DR -> (D,R)
        | BN -> (B,N) | BoN -> (B+offset,N) | HN -> (H,N) | XoN -> (X-offset,N) | XN -> (X,N) | TN -> (T,N)
        | Mid(p1, p2) -> this.rewritePoint (Interp(p1, p2, 0.5))
        | Interp(p1, p2, f) -> let y1, x1 = this.rewritePoint p1
                               let y2, x2 = this.rewritePoint p2
                               (y1+int(float(y2-y1)*f), x1+int(float(x2-x1)*f))
    
    member this.getXY offset p =
        let xyOffset = if offset then this.Axes.thickness else 0
        let italicFraction = if offset then this.Axes.italic_fraction else 0.0
        let y, x = this.rewritePoint p
        (x+xyOffset + int(italicFraction * float(y+xyOffset)), y+xyOffset)

    static member dotToClosedCurve x y r =
        ClosedCurve([(YX(y-r,x), G2); (YX(y,x+r), G2);
                     (YX(y+r,x), G2); (YX(y,x-r), G2)])

    //Straights: AEFHIKLMNTVWXYZklvwxyz147/=[]\`|*"'
    //Dots: ij:;!?
    //Curves: COScos36890()~
    //LeftUpright: BDPRb mnpr 
    //RightUpright: GJadgq
    //Other: QUefhtu25@#$€£_&-+{}%

    member this.getGlyph e =

        match e with

        // TODO: 
        // !"#£$%&'()*+,-./
        // 0123456789
        // :;<=>?@
        // [\]^_` {|}~
        // | Glyph('') -> 

        | Glyph('!') -> EList([Line(TL, ML); Dot(BL)])

        | Glyph('0') -> EList([ClosedCurve([(HL, G2); (BC, G2); (HR, G2); (TC, G2)]); Line(TR,BL)])
        | Glyph('1') -> PolyLine([YX(T-offset,L); YX(T,L+offset); YX(B, L+offset)])
        | Glyph('2') -> OpenCurve([(ToL, Start); (TLo, G2); (ToR, G2); (MC, CurveToLine); (BL, Corner); (BR, End)])
        | Glyph('3') -> EList([OpenCurve([(ToL, Corner); (YX(T,R-offset), G2); (Mid(TR, HR), G2); (HC, G2)]);
                              OpenCurve([(HC, G2); (Mid(HR, BR), G2); (BLo, G2); (BoL, End)])])
        | Glyph('4') -> let X4 = X/4 in PolyLine([BN; TN; YX(X4,L); YX(X4,R)])
        //| Glyph('5') -> OpenCurve([(TR, Start); (TL, Corner); (YX(X-T/50,L), Corner); (XC, G2); (MR, G2); (BC, G2); (BoL, End)])
        | Glyph('5') -> OpenCurve([(TR, Start); (TL, Corner); (XL, Corner); (XC, G2); (MR, G2); (BC, G2); (BoL, End)])
        | Glyph('6') -> OpenCurve([(ToR, Start); (TC, G2); (HL, G2); (BC, G2); (MR, G2); (XC, G2); (HL, End)])
        | Glyph('7') -> PolyLine([TL; TR; BLo])
        | Glyph('8') -> let M = T*6/10
                        ClosedCurve([(TC, G2); (Mid(TL,HL), G2); 
                                   (YX(M*11/10,C-offset), G2); (YX(M*9/10,C+offset), G2); 
                                   (Mid(HR,BR), G2); (BC, G2); (Mid(HL,BL), G2);
                                   (YX(M*9/10,C-offset), G2); (YX(M*11/10,C+offset), G2); 
                                   (Mid(TR,HR), G2)])
        | Glyph('9') -> OpenCurve([(BLo, Start); (HR, G2); (TC, G2); (Mid(TL,HL), G2); (HC, G2); (Mid(TR,HR), End)])

        | Part("adgqLoop") -> ClosedCurve([(XoR, Corner); (XC, G2); (ML, G2); (BC, G2); (BoR, Corner)])
        | Glyph('A') -> let f = float(H/2)/float(T)
                        EList([PolyLine([BL; TC; BR]); PolyLine([BL; Interp(BL,TC,f); Interp(BR,TC,f); BR])])
        | Glyph('a') -> EList([Line(XR, BR); Part("adgqLoop")])
        | Glyph('B') -> EList([Glyph('P'); OpenCurve([(HL, Corner); (HC, LineToCurve); (Mid(HR, BR), G2); (BC, CurveToLine); (BL, End)])])
        | Glyph('b') -> EList([Line(BL, TL); OpenCurve([(XoL, Start); (XC, G2); (MR, G2); (BC, G2); (BoL, End)])])
        | Glyph('C') -> OpenCurve([(ToR, Start); (TC, G2); (HL, G2); (BC, G2); (BoR, End)])
        | Glyph('c') -> OpenCurve([(XoR, Start); (XC, G2); (ML, G2); (BC, G2); (BoR, End)])
        | Glyph('D') -> ClosedCurve([(BL, Corner); (TL, Corner); (TLo, LineToCurve);
                                     (YX(H+offset,R), CurveToLine); (YX(H-offset,R), LineToCurve); (BLo, CurveToLine)])
        | Glyph('d') -> EList([Line(BR, TR); Part("adgqLoop")])
        | Glyph('E') -> EList([PolyLine([TR; TL; BL; BR]); Line(HL, HR)])
        | Glyph('e') -> OpenCurve([(ML, Start); (MR, Corner); (YX(M+offset,R), G2); (XC, G2); (ML, G2);
                        //(YX(B,C+offset), G2); (BoR, End)])
                        (BC, G2); (BoR, End)])
        | Glyph('F') -> EList([PolyLine([TR; TL; BL]); Line(HL, HRo)])
        | Glyph('f') -> EList([OpenCurve([(TC, Start); (XL, CurveToLine); (BL, End)]); Line(XL, XC)])
        | Glyph('G') -> OpenCurve([(ToR, G2); (TC, G2); (HL, G2); (BC, G2); (HoR, CurveToLine); (HR, Corner); (HC, End)])
        | Glyph('g') -> EList([Part("adgqLoop");
                              OpenCurve([(XR, Corner); (BR, LineToCurve); (DC, G2); (DoL, End)])])
        | Glyph('H') -> EList([Line(BL, TL); Line(HL, HR); Line(BR, TR)])
        | Glyph('h') -> EList([Line(BL, TL); OpenCurve([(XoL, Start); (XC, G2); (MR, CurveToLine); (BR, End)])])
        | Glyph('I') -> Line(BL, TL)
        | Glyph('i') -> EList([Line(XL, BL)
                               Dot(YX(dotHeight,L))])
        | Glyph('J') -> OpenCurve([(TL, Corner); (TR, Corner); (HR, LineToCurve); (BC, G2); (BoL, End)])
        | Glyph('j') -> EList([OpenCurve([(XR, Corner); (BR, LineToCurve); (DC, G2); (DoL, End)])
                               Dot(YX(dotHeight,R))])
        | Glyph('K') -> EList([PolyLine([TR; HL; BL]); PolyLine([TL; HL; BR])])
        | Glyph('k') -> EList([Line(TL, BL); PolyLine([YX(X,N); ML; YX(B,N)])])
        | Glyph('L') -> PolyLine([TL; BL; BR])
        | Glyph('l') -> OpenCurve([(TL, Corner); (ML, LineToCurve); (BC, G2)])
        | Glyph('M') -> PolyLine([BL; TL; YX(B,R*3/4); YX(T,R*3/2); YX(B,R*3/2)])
        | Glyph('m') -> EList([Glyph('n');
                              OpenCurve([(YX(X-offset,N), Start); (YX(X,N+C), G2); (YX(M,N+N), CurveToLine); (YX(B,N+N), End)])])
        | Glyph('N') -> PolyLine([BL; TL; BR; TR])
        | Glyph('n') -> EList([Line(XL,BL)
                               OpenCurve([(BL, Start); (XoL, Corner); (XC, G2); (YX(M,N), CurveToLine); (BN, End)])])
        | Glyph('O') -> ClosedCurve([(HL, G4); (BC, G2); (HR, G4); (TC, G2)])
        | Glyph('o') -> ClosedCurve([(XC, G2); (ML, G2); (BC, G2); (MR, G2)])
        | Glyph('P') -> OpenCurve([(BL, Corner); (TL, Corner); (TC, LineToCurve); (Mid(TR, HR), G2); (HC, CurveToLine); (HL, End)])
        | Glyph('p') -> EList([Line(XL, DL)
                               OpenCurve([(XoL, Start); (XC, G2); (MR, G2); (BC, G2); (BoL, End)])])
        | Glyph('Q') -> EList([Glyph('O'); Line(Mid(HC, BR), BR)])
        | Glyph('q') -> EList([Line(XR, DR); Part("adgqLoop")])
        | Glyph('R') -> EList([Glyph('P'); PolyLine([HL; HC; BR])])
        | Glyph('r') -> EList([Line(BL,XL)
                               OpenCurve([(BL, Start); (XoL, Corner); (XC, G2); (XoN, End)])])
        | Glyph('S') -> OpenCurve([(ToR, G2); (TC, G2); (Mid(TL,HL), G2); 
                                   (YX(H*11/10,C-offset), G2); (YX(H*9/10,C+offset), G2); 
                                   (Mid(HR,BR), G2); (BC, G2); (BoL, End)])
        | Glyph('s') -> let X14, X2, X34, cOffset = X/4, X/2, X*3/4, C/8
                        OpenCurve([(YX(X-offset,R), G2); (YX(X, C-offset/2), G2); (YX(X34,L), G2);
                                   (YX(X2,C-cOffset), CurveToLine); (YX(X2,C+cOffset), LineToCurve); 
                                   (YX(X14,R), G2); (YX(B,C+offset/2), G2); (YX(B+offset,L), End)])
        | Glyph('T') -> EList([Line(TL, TR); Line(TC, BC)])
        | Glyph('t') -> EList([Glyph('l'); Line(XL,XC)])
        | Glyph('U') -> OpenCurve([(TL, Corner); (HL, LineToCurve); (BC, G2); (HR, CurveToLine); (TR, End)])
        | Glyph('u') -> EList([Line(BN,XN)
                               OpenCurve([(BoN, Start); (BC, G2); (ML, CurveToLine); (XL, End)])])
        | Glyph('V') -> PolyLine([TL; BC; TR])
        | Glyph('v') -> PolyLine([XL; BC; XR])
        | Glyph('W') -> PolyLine([TL; BC; TR; YX(B,R+R/2); YX(T,R+R)])
        | Glyph('w') -> PolyLine([XL; YX(B,N/2); XN; YX(B,N+N/2); YX(X,N+N)])
        | Glyph('X') -> EList([Line(TL,BR); Line(TR,BL)])
        | Glyph('x') -> EList([Line(XL,BR); Line(XR,BL)])
        | Glyph('Y') -> EList([PolyLine([TL; HC; TR]); Line(HC,BC)])
        | Glyph('y') -> EList([OpenCurve([(XR, Corner); (BR, LineToCurve); (DC, G2); (DoL, End)])
                               OpenCurve([(XL, Corner); (ML, LineToCurve); (BC, G2); (MR, CurveToLine); (XR, End)])])
        | Glyph('Z') -> PolyLine([TL; TR; BL; BR])
        | Glyph('z') -> PolyLine([XL; XR; BL; BR])

        | Glyph(' ') -> Space

        //default
        | Glyph(c) -> printfn "Glyph %c not defined" c
                      Dot(XC)
        | any -> any

    member this.reduce  e =
        match e with
        | Line(p1, p2) -> OpenCurve([(p1, Start); (p2, End)]) |> this.reduce
        | PolyLine(points) -> let a = Array.ofList points
                              OpenCurve([for i in 0 .. a.Length-1 do yield (a.[i], if i=(a.Length-1) then End else Corner)])
                              |> this.reduce
        | OpenCurve(curvePoints) -> OpenCurve([for p, t in curvePoints do YX(this.rewritePoint p), t])
        | ClosedCurve(curvePoints) -> ClosedCurve([for p, t in curvePoints do YX(this.rewritePoint p), t])
        | Dot(p) -> Dot(YX(this.rewritePoint(p)))
        | EList(el) -> EList(List.map this.reduce el)
        | Space -> Space
        | e -> this.getGlyph(e) |> this.reduce

    member this.width e =
        let thickness = this.Axes.thickness
        let nonItalic = Font({this.Axes with italic_fraction=0.0})
        let maxX curvePoints = this.Axes.leading + thickness*2 + List.fold max 0 (List.map (fst >> (nonItalic.getXY false) >> fst) curvePoints)
        match this.reduce(e) with
        | OpenCurve(curvePoints) -> maxX curvePoints
        | ClosedCurve(curvePoints) -> maxX curvePoints
        | Dot(p) -> thickness*2 + fst (this.getXY false p)
        | EList(el) -> List.fold max 0 (List.map this.width el)
        | Space -> this.Axes.height / 4  //according to https://en.wikipedia.org/wiki/Whitespace_character#Variable-width_general-purpose_space
        | _ -> invalidArg "e" (sprintf "Unreduced element %A" e)

    member this.elementToSpirosOffset offset e =
        let getXY = this.getXY offset
        let makeSCP p t = { SCP.X=float(fst(getXY(p))); Y=float(snd(getXY p)); Type=t}
        match this.reduce(e) with
        | OpenCurve(curvePoints) ->
            let scps = [for p, t in curvePoints do makeSCP p t]
            let segments = Spiro.SpiroCPsToSegments (Array.ofList scps) false
            match segments with
            | Some segs -> [SpiroOpenCurve(scps, Array.toList segs)]
            | None -> [SpiroSpace]
        | ClosedCurve(curvePoints) ->
            let scps = [for p, t in curvePoints do makeSCP p t]
            let segments = Spiro.SpiroCPsToSegments (Array.ofList scps) true
            match segments with
            | Some segs -> [SpiroClosedCurve(scps, Array.toList segs)]
            | None -> [SpiroSpace]
        | Dot(p) -> [SpiroDot(p)]
        | EList(el) -> List.map this.getGlyph el |> List.collect (this.elementToSpirosOffset offset)
        | Space -> [SpiroSpace]
        | _ -> invalidArg "e" (sprintf "Unreduced element %A" e) 

    member this.elementToSpiros = this.elementToSpirosOffset false

    static member offsetSegment (seg : SpiroSegment) (lastSeg : SpiroSegment) reverse dist =
        ///normalise angle to between PI/2 and -PI/2
        let norm x = if x>PI then x-PI*2.0 else if x<(-PI) then x+PI*2.0 else x
        let newType = if reverse then 
                            match seg.Type with
                            | SpiroPointType.Left -> SpiroPointType.Right
                            | SpiroPointType.Right -> SpiroPointType.Left
                            | x -> x
                       else seg.Type
        let angle = if reverse then -PI/2.0 else PI/2.0
        match seg.Type with
        | SpiroPointType.Corner ->
            let th1, th2 = norm(lastSeg.Tangent2 + angle), norm(seg.Tangent1 + angle)
            let bend = norm(th2 - th1)
            if (not reverse && bend < -PI/2.0) || (reverse && bend > PI/2.0) then
                //two points on sharp outer bend
                [(seg.Offset th1 dist, newType);
                 (seg.Offset th2 dist, newType)]
            else //right angle or more outer bend or inner bend
                let offset = min (min (dist/cos (bend/2.0)) seg.seg_ch) lastSeg.seg_ch
                if (dist/cos (bend/2.0)) > seg.seg_ch || (dist/cos (bend/2.0)) > lastSeg.seg_ch then
                    if Set.ofList [seg.Type; lastSeg.Type] = Set.ofList [Corner; G2] then
                        printfn "corner/curve bend"
                    printfn "inner bend %f offset %f chords %f %f " bend (dist/cos (bend/2.0)) seg.seg_ch lastSeg.seg_ch
                [(offsetPoint seg.X seg.Y (th1 + bend/2.0) offset, newType)]
        | SpiroPointType.Right ->
            //not sure why lastSeg.seg_th is different from (fst (tangents seg)) here
            [(seg.Offset (norm (lastSeg.seg_th + angle)) dist, newType)]
        | SpiroPointType.Left ->
            [(seg.Offset (norm (seg.seg_th + angle)) dist, newType)]
        | _ ->
            [(seg.Offset (seg.Tangent1 + angle) dist, newType)]

    static member offsetSegments (segments : list<SpiroSegment>) start endP reverse closed dist =
        [for i in start .. endP do
            let seg = segments.[i]
            let angle = if reverse then -PI/2.0 else PI/2.0
            if i = 0 then
                if closed then
                    let lastSeg = segments.[segments.Length-2]
                    Font.offsetSegment seg lastSeg reverse dist
                else
                    [(seg.Offset (seg.Tangent1 + angle) dist, seg.Type)]
            elif i = segments.Length-1 then
                if not closed then
                    let lastSeg = segments.[i-1]
                    [(seg.Offset (lastSeg.Tangent2 + angle) dist, seg.Type)]
            else
                let lastSeg = segments.[i-1]
                Font.offsetSegment seg lastSeg reverse dist
        ] |> List.collect id

    member this.getSansOutlines e = 
        let spiros = this.elementToSpirosOffset true e
        let thickness = float this.Axes.thickness
        let offsetPointCap X Y theta = offsetPoint X Y theta (thickness * sqrt 2.0)
        let offsetMidSegments segments reverse =
            Font.offsetSegments segments 1 (segments.Length-2) reverse false thickness
        let startCap (seg : SpiroSegment) =
            [(offsetPointCap seg.X seg.Y (seg.Tangent1 - PI * 0.75), Corner);
             (offsetPointCap seg.X seg.Y (seg.Tangent1 + PI * 0.75), Corner)]
        let endCap (seg : SpiroSegment) (lastSeg : SpiroSegment) = 
            [(offsetPointCap seg.X seg.Y (lastSeg.Tangent2 + PI/4.0), Corner);
             (offsetPointCap seg.X seg.Y (lastSeg.Tangent2 - PI/4.0), Corner)]
        let spiroToOutline spiro =
            match spiro with
            | SpiroOpenCurve(_, segments) ->
                let points = startCap segments.[0]
                             @ offsetMidSegments segments false
                             @ endCap segments.[segments.Length-1] segments.[segments.Length-2]
                             @ (offsetMidSegments segments true |> List.rev)
                [ClosedCurve(points)]
            | SpiroClosedCurve(_, segments) ->
                [ClosedCurve(Font.offsetSegments segments 0 (segments.Length-2) false true thickness);
                 ClosedCurve(Font.offsetSegments segments 0 (segments.Length-2) true true thickness |> List.rev)]
            | SpiroDot(p) ->
                let x,y = this.getXY true p
                [Font.dotToClosedCurve x y this.Axes.thickness]
            | SpiroSpace -> [Space]
        EList(List.collect spiroToOutline spiros)

    member this.getStroked e = 
        let spiros = this.elementToSpirosOffset true e
        let spiroToLines spiro =
            let thicknessby3 = float this.Axes.thickness/3.0
            match spiro with
            | SpiroOpenCurve(_, segments) ->
                [for t in -3..2..3 do 
                    OpenCurve(Font.offsetSegments segments 0 (segments.Length-1) false false (thicknessby3*float t))]
            | SpiroClosedCurve(_, segments) ->
                [for t in -3..2..3 do 
                    ClosedCurve(Font.offsetSegments segments 0 (segments.Length-2) false true (thicknessby3*float t))]
            | SpiroDot(p) ->
                let x,y = this.getXY true p
                [Font.dotToClosedCurve x y this.Axes.thickness; Font.dotToClosedCurve x y (this.Axes.thickness/2)]
            | SpiroSpace -> [Space]
        EList(List.collect spiroToLines spiros) |> Font({this.Axes with thickness = 2}).getSansOutlines

    member this.getScratches e = 
        let spiros = this.elementToSpirosOffset true e
        let spiroToScratches spiro =
            let thicknessby3 = float this.Axes.thickness/3.0
            match spiro with
            | SpiroOpenCurve(_, segments) ->
                [for t in -3..3..3 do 
                    OpenCurve(Font.offsetSegments segments 0 (segments.Length-1) false false (thicknessby3*float t))]
            | SpiroClosedCurve(_, segments) ->
                [for t in -3..3..3 do 
                    ClosedCurve(Font.offsetSegments segments 0 (segments.Length-2) false true (thicknessby3*float t))]
            | SpiroDot(p) -> [Dot(p)]
            | SpiroSpace -> [Space]
        
        let spiroToScratchOutlines spiro =
            let thicknessby3 = float this.Axes.thickness/3.0
            let offsetPointCap X Y theta = offsetPoint X Y theta (thicknessby3 * sqrt 2.0)
            let offsetMidSegments segments reverse =
                Font.offsetSegments segments 1 (segments.Length-2) reverse false thicknessby3
            let startCap (seg : SpiroSegment) =
                [(seg.Offset (seg.Tangent1 - PI * 0.90) (thicknessby3*3.0), Corner);
                //[(offsetPointCap seg.X seg.Y (seg.Tangent1 - PI * 0.75), Corner);
                 //(offsetPointCap seg.X seg.Y (seg.Tangent1 + PI), Corner);
                 (offsetPointCap seg.X seg.Y (seg.Tangent1 + PI * 0.75), Corner)]
            let endCap (seg : SpiroSegment) (lastSeg : SpiroSegment) = 
                [(offsetPointCap seg.X seg.Y lastSeg.Tangent2, Corner)]
            match spiro with
            | SpiroOpenCurve(_, segments) ->
                let points = startCap segments.[0]
                             @ offsetMidSegments segments false
                             @ endCap segments.[segments.Length-1] segments.[segments.Length-2]
                             @ (offsetMidSegments segments true |> List.rev)
                [ClosedCurve(points)]
            | SpiroClosedCurve(_, segments) ->
                [ClosedCurve(Font.offsetSegments segments 0 (segments.Length-2) false true thicknessby3);
                 ClosedCurve(Font.offsetSegments segments 0 (segments.Length-2) true true thicknessby3 |> List.rev)]
            | SpiroDot(p) -> [Dot(p)]
            | SpiroSpace -> [Space]
        EList(List.collect spiroToScratches spiros |> List.collect this.elementToSpiros |> List.collect spiroToScratchOutlines)

    member this.toSvgBezierCurve spiro = 
        match spiro with
        | SpiroOpenCurve(scps, _) ->
            let bc = PathBezierContext()
            Spiro.SpiroCPsToBezier (Array.ofList scps) false bc |> ignore
            bc.ToString
        | SpiroClosedCurve(scps, _) ->
            let bc = PathBezierContext()
            Spiro.SpiroCPsToBezier (Array.ofList scps) true bc |> ignore
            bc.ToString
        | SpiroDot(p) -> let x, y = this.getXY true p
                         svgCircle x y this.Axes.thickness
        | SpiroSpace -> ""

    member this.getOutline =
        if this.Axes.stroked then
            this.getStroked
        elif this.Axes.scratches then
            this.getScratches
        elif this.Axes.outline then
            this.getSansOutlines
        else 
            id

    member this.getSvgCurves element offsetX offsetY strokeWidth =
        let spiros = this.getOutline element |> this.elementToSpiros
        let spirosPath = this.elementToSpiros element
        let fillrule = match spirosPath.[0] with
                        | SpiroClosedCurve(_) -> "evenodd"
                        | _ -> "nonzero"
        let svg = spiros |> List.map this.toSvgBezierCurve |> concatLines
        let fillStyle = if this.Axes.filled then "#000000" else "none"
        sprintf "<path d='%s' transform='translate(%d,%d) scale(1,-1)' " svg offsetX offsetY +
            sprintf "style='fill:%s;fill-rule:%s;stroke:#000000;stroke-width:%d'/>\n" fillStyle fillrule strokeWidth

    member this.getSvgKnots element offsetX offsetY =
        //Get circles highlighting the knots (defined points on the spiro curves)
        let toSvgPoints (spiro : SpiroElement) : string = 
            let thickness = this.Axes.thickness
            let circle(x,y) = svgCircle (int x + thickness) (int y + thickness) 50
            match spiro with
            | SpiroOpenCurve(scps, _) -> scps |> List.map (fun scp -> circle(scp.X, scp.Y)) |> concatLines
            | SpiroClosedCurve(scps, _) -> scps |> List.map (fun scp -> circle(scp.X, scp.Y)) |> concatLines
            | SpiroDot(p) -> let x,y = this.getXY false p in circle(float(x), float(y))
            | SpiroSpace -> ""
        let svg = element |> this.elementToSpiros |> List.map toSvgPoints |> concatLines
        // small red circles
        sprintf "<!-- points --><path d='%s' transform='scale(1,-1) translate(%d,%d)' " svg offsetX offsetY + 
            "style='fill:none;stroke:#ffaaaa;stroke-width:10'/>\n"

    member this.charToSvg ch offsetX offsetY showKnots =
        let element = Glyph(ch)
        sprintf "<!-- %c -->\n\n" ch +
        this.getSvgCurves element offsetX offsetY 5 +
            (if showKnots then this.getSvgKnots element offsetX offsetY else "")


    member this.stringWidth (str : string) =
        List.sum ([for ch in str do this.width (Glyph(ch))])


    member this.stringToSvg (str : string) offsetX offsetY showKnots =
        let widths = [for ch in str do this.width (Glyph(ch))]
        let offsetXs = List.scan (+) offsetX widths
        String.concat "\n"
            [for c in 0 .. str.Length - 1 do
                printfn "%c" str.[c]
                yield this.charToSvg str.[c] (offsetXs.[c]) offsetY showKnots]


let svgText x y text =
    sprintf """<text x='%d' y='%d' font-size='200'>%s</text>\n""" x y text

let toSvgDocument height width svg =
    sprintf """<svg xmlns='http://www.w3.org/2000/svg'
            viewBox='0 0 %d %d'>
            <g id='layer1'>
            %s
            </g>
            </svg>""" width height svg

//end module

