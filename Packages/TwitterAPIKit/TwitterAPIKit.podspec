Pod::Spec.new do |s|
  s.name         = 'TwitterAPIKit'
  s.version      = '1.0.0'
  s.summary      = 'Local Swift Package for testing'
  s.homepage     = 'https://example.com'
  s.license      = { :type => 'MIT' }
  s.author       = { 'You' => 'you@example.com' }
  s.source       = { :path => '.' }
  s.source_files = 'Sources/**/*.swift'
  s.swift_version = '5.0'
  s.platform     = :ios, '12.0'
end

