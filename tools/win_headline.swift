// Prepare an imagegen wordmark for the existing sprite importer. Some image
// exports bake their neutral checkerboard preview into RGB instead of alpha.
// Remove connected neutral matte regions, preserving bright letter highlights.
// Usage: swift tools/win_headline.swift source.png tools/art/win/victory_headline.png
import Foundation
import CoreGraphics
import ImageIO
import UniformTypeIdentifiers

guard CommandLine.arguments.count == 3 else { fatalError("Expected source and destination PNG paths") }
let input = URL(fileURLWithPath: CommandLine.arguments[1])
let output = URL(fileURLWithPath: CommandLine.arguments[2])
guard let source = CGImageSourceCreateWithURL(input as CFURL, nil),
      let image = CGImageSourceCreateImageAtIndex(source, 0, nil) else { fatalError("Cannot read source PNG") }
let width = image.width, height = image.height, count = width * height
var pixels = [UInt8](repeating: 0, count: count * 4)
let space = CGColorSpaceCreateDeviceRGB()
let info = CGBitmapInfo.byteOrder32Big.rawValue | CGImageAlphaInfo.premultipliedLast.rawValue
pixels.withUnsafeMutableBytes { raw in
    guard let context = CGContext(data: raw.baseAddress, width: width, height: height,
        bitsPerComponent: 8, bytesPerRow: width * 4, space: space, bitmapInfo: info)
        else { fatalError("Cannot decode PNG") }
    context.draw(image, in: CGRect(x: 0, y: 0, width: width, height: height))
}
var visited = [Bool](repeating: false, count: count)
var matte = [Bool](repeating: false, count: count)
let hasAlpha = (0..<count).contains { pixels[$0*4+3] < 255 }
func neutral(_ i: Int) -> Bool {
    let r = Int(pixels[i*4]), g = Int(pixels[i*4+1]), b = Int(pixels[i*4+2])
    return max(r,g,b) - min(r,g,b) < 20
}
for start in 0..<count where !hasAlpha && !visited[start] && neutral(start) {
    var region = [start], head = 0, brightness = 0
    visited[start] = true
    while head < region.count {
        let i = region[head]; head += 1
        brightness += Int(pixels[i*4]) + Int(pixels[i*4+1]) + Int(pixels[i*4+2])
        let x = i % width, y = i / width
        let neighbours = [x > 0 ? i-1 : -1, x+1 < width ? i+1 : -1,
                          y > 0 ? i-width : -1, y+1 < height ? i+width : -1]
        for next in neighbours where next >= 0 && !visited[next] && neutral(next) {
            visited[next] = true; region.append(next)
        }
    }
    if brightness / (region.count * 3) < 235 {
        for i in region { matte[i] = true }
    }
}
var left = width, right = 0, top = height, bottom = 0
for i in 0..<count {
    if matte[i] {
        for c in 0..<4 { pixels[i*4+c] = 0 }
    } else if pixels[i*4+3] > 0 {
        let x = i % width, y = i / width
        left = min(left,x); right = max(right,x); top = min(top,y); bottom = max(bottom,y)
    }
}
guard right > left && bottom > top else { fatalError("No lettering found") }
left = max(0,left-8); top = max(0,top-8)
right = min(width-1,right+8); bottom = min(height-1,bottom+8)
let data = Data(pixels) as CFData
guard let provider = CGDataProvider(data: data),
      let rgba = CGImage(width: width, height: height, bitsPerComponent: 8, bitsPerPixel: 32,
        bytesPerRow: width*4, space: space, bitmapInfo: CGBitmapInfo(rawValue: info),
        provider: provider, decode: nil, shouldInterpolate: true, intent: .defaultIntent),
      let cropped = rgba.cropping(to: CGRect(x: left, y: top, width: right-left+1, height: bottom-top+1)),
      let destination = CGImageDestinationCreateWithURL(output as CFURL, UTType.png.identifier as CFString, 1, nil)
      else { fatalError("Cannot create RGBA sprite") }
CGImageDestinationAddImage(destination, cropped, nil)
guard CGImageDestinationFinalize(destination) else { fatalError("Cannot save sprite") }
print("Prepared \(cropped.width)x\(cropped.height) RGBA wordmark: \(output.path)")
