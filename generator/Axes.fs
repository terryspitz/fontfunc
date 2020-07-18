module Axes

/// UI element type for an axis
type Controls = 
    | Range of from : int * upto : int
    | FracRange of from : float * upto : float
    | Checkbox

// Variable which define the font characteristics (named after Variable Font terminology)
type Axes = {
    width : int                 //width of normal glyph
    height : int                //capital height
    x_height : int              //height of lower case
    thickness : int             //stroke width
    contrast : float            //make vertical lines thicker
    roundedness : int           //roundedness
    // overshoot : int          //curves are larger by this amount to compensate for looking smaller
    tracking : int              //gap between glyphs
    leading : int               //gap between lines
    monospace : float           //fraction to interpolate widths to monospaces
    italic : float              //fraction to sheer glyphs
    serif : int                 //serif size
    end_bulb : float            //fraction of thickness to apply curves to endcaps
    flare : float               //end caps expand by this amount
    axis_align_caps : bool      //round angle of caps to horizontal/vertical
    //spine : bool              //show the single width glyph, use with outline off or filled off
    outline : bool              //use thickness to expand stroke width
    stroked : bool              //each stroke is 4 parallel lines
    scratches : bool            //horror/paint strokes font
    filled : bool               //(svg only) filled or empty outlines
    spline_not_spiro : bool      //use original Spiro (false) or new spline-research splines (true)
    show_knots : bool           //show small circles for the points used to define lines/curves
    joints : bool               //check joints to turn off serifs
    constraints : bool               //check joints to turn off serifs
} with
    static member DefaultAxes = { 
        width = 300
        height = 600
        x_height = 400
        thickness = 30
        contrast = 0.05
        roundedness = 100
        tracking = 40
        leading = 50
        monospace = 0.0
        italic = 0.0
        serif = 0
        end_bulb = 0.0
        flare = 0.0
        axis_align_caps = true
        outline = true
        stroked = false
        scratches = false
        filled = true
        spline_not_spiro = true
        show_knots = false
        joints = true
        constraints = false
    }
    static member controls = Map.ofList [
        "width", Range(100, 1000)
        "height", Range(400, 1000)
        "x_height", Range(0, 1000)
        "thickness", Range(1, 200)
        "contrast", FracRange(-0.5, 0.5)
        "roundedness", Range(0, 300)
        "tracking", Range(0, 200)
        "leading", Range(-100, 200)
        "monospace", FracRange(0.0, 1.0)
        "italic", FracRange(0.0, 1.0)
        "serif", Range(0, 70)
        "end_bulb", FracRange(-1.0, 3.0)
        "flare", FracRange(-1.0, 1.0)
        "axis_align_caps", Checkbox
        "outline", Checkbox
        "stroked", Checkbox
        "scratches", Checkbox
        "filled", Checkbox
        "spline_not_spiro", Checkbox
        "show_knots", Checkbox
        "joints", Checkbox
        "constraints", Checkbox
    ]